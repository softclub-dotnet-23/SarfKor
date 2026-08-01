using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Stores;

namespace Application.Stores.Commands.ConfirmStoreOwnerInvitation;

public sealed class ConfirmStoreOwnerInvitationCommandHandler(
    IStoreOwnerInvitationRepository invitationRepository,
    IStoreRepository storeRepository,
    IAuthService authService,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ConfirmStoreOwnerInvitationCommand, ConfirmStoreOwnerInvitationResult>
{
    private const string StorePartnerRole = "StorePartner";
    private const int MaxAttempts = 5;

    public async Task<ConfirmStoreOwnerInvitationResult> Handle(ConfirmStoreOwnerInvitationCommand command, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetPendingByEmailAsync(command.Email, cancellationToken);
        // Covers "never invited," "expired," and "already used" with one indistinguishable answer.
        if (invitation is null)
            return new ConfirmStoreOwnerInvitationResult(ConfirmStoreOwnerInvitationOutcome.InvalidOrExpiredCode, null, null);

        if (invitation.AttemptCount >= MaxAttempts)
            return new ConfirmStoreOwnerInvitationResult(ConfirmStoreOwnerInvitationOutcome.TooManyAttempts, null, null);

        if (!OtpCode.Matches(command.Email, command.Code, invitation.CodeHash))
        {
            invitation.AttemptCount++;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new ConfirmStoreOwnerInvitationResult(ConfirmStoreOwnerInvitationOutcome.InvalidOrExpiredCode, null, null);
        }

        // A race since the invite was sent (self-registered independently in the meantime).
        if (await authService.FindUserIdByEmailAsync(command.Email, cancellationToken) is not null)
            return new ConfirmStoreOwnerInvitationResult(ConfirmStoreOwnerInvitationOutcome.EmailAlreadyRegistered, null, null);

        // Must happen before the Store is created — Store.OwnerUserId is a non-nullable required
        // field and needs the new account's id.
        var registerResult = await authService.RegisterAsync(command.Email, command.Password, cancellationToken);
        if (registerResult is null)
            return new ConfirmStoreOwnerInvitationResult(ConfirmStoreOwnerInvitationOutcome.RegistrationFailed, null, null);

        var store = new Store
        {
            OwnerUserId = registerResult.UserId,
            Name = invitation.StoreName,
            Address = invitation.Address,
            Location = invitation.Location,
            // Approved, not Pending — an Admin already vetted this store before sending the invite.
            Status = StoreStatus.Approved
        };
        storeRepository.Add(store);

        await authService.AssignRoleAsync(registerResult.UserId, StorePartnerRole, cancellationToken);

        invitation.AcceptedAt = DateTimeOffset.UtcNow;

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = invitation.InvitedByUserId,
            Action = "StoreOwnerInvitation.Confirmed",
            EntityType = nameof(StoreOwnerInvitation),
            EntityId = invitation.Id,
            Details = command.Email,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Not RegisterAsync's own tokens: those were minted before AssignRoleAsync ran above, so
        // they'd carry a stale "User"-only role claim — re-authenticating now mints a token that
        // reflects the role actually granted (same reasoning as AcceptStoreEmployeeInvitationCommandHandler).
        var auth = await authService.LoginAsync(command.Email, command.Password, null, null, cancellationToken);

        return new ConfirmStoreOwnerInvitationResult(ConfirmStoreOwnerInvitationOutcome.Confirmed, auth, store.Id);
    }
}
