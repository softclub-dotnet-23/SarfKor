using Application.Abstractions;
using Application.Common;
using Domain.Identity;
using Domain.Stores;

namespace Application.Stores.Commands.AcceptStoreEmployeeInvitation;

public sealed class AcceptStoreEmployeeInvitationCommandHandler(
    IStoreEmployeeInvitationRepository invitationRepository,
    IStoreEmployeeRepository storeEmployeeRepository,
    IUserProfileRepository userProfileRepository,
    IAuthService authService,
    IUnitOfWork unitOfWork) : ICommandHandler<AcceptStoreEmployeeInvitationCommand, AcceptStoreEmployeeInvitationResult>
{
    private const string StorePartnerRole = "StorePartner";

    public async Task<AcceptStoreEmployeeInvitationResult> Handle(AcceptStoreEmployeeInvitationCommand command, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByTokenHashAsync(InviteToken.Hash(command.Token), cancellationToken);
        if (invitation is null)
            return new AcceptStoreEmployeeInvitationResult(AcceptStoreEmployeeInvitationOutcome.NotFound, null);

        switch (invitation.Status)
        {
            case StoreEmployeeInvitationStatus.Accepted:
                return new AcceptStoreEmployeeInvitationResult(AcceptStoreEmployeeInvitationOutcome.AlreadyAccepted, null);
            case StoreEmployeeInvitationStatus.Revoked:
                return new AcceptStoreEmployeeInvitationResult(AcceptStoreEmployeeInvitationOutcome.Revoked, null);
        }

        if (invitation.IsEffectivelyExpired(DateTimeOffset.UtcNow))
            return new AcceptStoreEmployeeInvitationResult(AcceptStoreEmployeeInvitationOutcome.Expired, null);

        var userId = await authService.FindUserIdByEmailAsync(invitation.Email, cancellationToken);
        var accountAlreadyExisted = userId is not null;

        if (userId is null)
        {
            if (string.IsNullOrEmpty(command.Password))
                return new AcceptStoreEmployeeInvitationResult(AcceptStoreEmployeeInvitationOutcome.PasswordRequired, null);

            // A brand-new account, created with the password the invitee just chose — reuses the
            // exact same path as self-registration, so it gets the same default "User" role too
            // (AssignRoleAsync below adds StorePartner on top of it, same as a normal cashier add).
            // emailPreVerified: true — clicking this invite link, sent only to invitation.Email,
            // already proves ownership; the account skips the separate registration-OTP step.
            var registerResult = await authService.RegisterAsync(invitation.Email, command.Password, emailPreVerified: true, cancellationToken);
            if (registerResult.Auth is null)
                return new AcceptStoreEmployeeInvitationResult(AcceptStoreEmployeeInvitationOutcome.RegistrationFailed, null);

            userId = registerResult.Auth.UserId;

            userProfileRepository.Add(new UserProfile { UserId = userId, DisplayName = command.DisplayName.Trim() });
        }

        var existingEmployments = await storeEmployeeRepository.GetByStoreIdAsync(invitation.StoreId, cancellationToken);
        if (!existingEmployments.Any(e => e.UserId == userId))
        {
            storeEmployeeRepository.Add(new StoreEmployee
            {
                StoreId = invitation.StoreId,
                UserId = userId,
                Role = invitation.Role,
                AddedAt = DateTimeOffset.UtcNow
            });
            await authService.AssignRoleAsync(userId, StorePartnerRole, cancellationToken);
        }

        invitation.Status = StoreEmployeeInvitationStatus.Accepted;
        invitation.AcceptedAt = DateTimeOffset.UtcNow;
        invitation.AcceptedUserId = userId;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        AuthResult? auth = null;
        if (!accountAlreadyExisted)
        {
            // Not RegisterAsync's own tokens: those were minted before AssignRoleAsync ran above,
            // so they'd carry a stale "User"-only role and RequireStore would bounce the new cashier
            // to onboarding instead of their store. Re-authenticating now mints a token that reflects
            // the role actually granted.
            auth = (await authService.LoginAsync(invitation.Email, command.Password!, null, null, cancellationToken)).Auth;
        }

        // An account for this email already existed (self-registered independently in the
        // meantime, or accepted a different store's invite before) — attach them as an employee,
        // but never touch that account's existing password from an email-link click, so no fresh
        // tokens to hand back; they log in normally.
        return new AcceptStoreEmployeeInvitationResult(
            accountAlreadyExisted ? AcceptStoreEmployeeInvitationOutcome.AccountAlreadyExisted : AcceptStoreEmployeeInvitationOutcome.Accepted,
            auth);
    }
}
