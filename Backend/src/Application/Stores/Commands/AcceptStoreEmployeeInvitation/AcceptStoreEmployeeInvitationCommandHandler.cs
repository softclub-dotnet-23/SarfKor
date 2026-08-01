using Application.Abstractions;
using Application.Common;
using Domain.Stores;

namespace Application.Stores.Commands.AcceptStoreEmployeeInvitation;

public sealed class AcceptStoreEmployeeInvitationCommandHandler(
    IStoreEmployeeInvitationRepository invitationRepository,
    IStoreEmployeeRepository storeEmployeeRepository,
    IAuthService authService,
    IUnitOfWork unitOfWork) : ICommandHandler<AcceptStoreEmployeeInvitationCommand, AcceptStoreEmployeeInvitationResult>
{
    private const string StorePartnerRole = "StorePartner";

    public async Task<AcceptStoreEmployeeInvitationResult> Handle(AcceptStoreEmployeeInvitationCommand command, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByTokenAsync(command.Token, cancellationToken);
        if (invitation is null || invitation.AcceptedAt is not null || invitation.ExpiresAt < DateTimeOffset.UtcNow)
            return new AcceptStoreEmployeeInvitationResult(AcceptStoreEmployeeInvitationOutcome.InvalidOrExpiredToken, null);

        var userId = await authService.FindUserIdByEmailAsync(invitation.Email, cancellationToken);
        var accountAlreadyExisted = userId is not null;

        if (userId is null)
        {
            // A brand-new account, created with the password the invitee just chose — reuses the
            // exact same path as self-registration, so it gets the same default "User" role too
            // (AssignRoleAsync below adds StorePartner on top of it, same as a normal cashier add).
            // emailPreVerified: true — clicking this invite link, sent only to invitation.Email,
            // already proves ownership; the account skips the separate registration-OTP step.
            var registerResult = await authService.RegisterAsync(invitation.Email, command.Password, emailPreVerified: true, cancellationToken);
            if (registerResult.Auth is null)
                return new AcceptStoreEmployeeInvitationResult(AcceptStoreEmployeeInvitationOutcome.RegistrationFailed, null);

            userId = registerResult.Auth.UserId;
        }

        var existing = await storeEmployeeRepository.GetByStoreIdAsync(invitation.StoreId, cancellationToken);
        if (!existing.Any(e => e.UserId == userId))
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

        invitation.AcceptedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        AuthResult? auth = null;
        if (!accountAlreadyExisted)
        {
            // Not RegisterAsync's own tokens: those were minted before AssignRoleAsync ran above,
            // so they'd carry a stale "User"-only role and RequireStore would bounce the new cashier
            // to onboarding instead of their store. Re-authenticating now mints a token that reflects
            // the role actually granted.
            auth = (await authService.LoginAsync(invitation.Email, command.Password, null, null, cancellationToken)).Auth;
        }

        // An account for this email already existed (self-registered independently in the
        // meantime) — attach them as an employee, but never touch that account's existing password
        // from an email-link click, so no fresh tokens to hand back; they log in normally.
        return new AcceptStoreEmployeeInvitationResult(
            accountAlreadyExisted ? AcceptStoreEmployeeInvitationOutcome.AccountAlreadyExisted : AcceptStoreEmployeeInvitationOutcome.Accepted,
            auth);
    }
}
