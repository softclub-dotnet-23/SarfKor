using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Identity;

namespace Application.Identity.Commands.ConfirmAdminInvitation;

public sealed class ConfirmAdminInvitationCommandHandler(
    IAdminInvitationRepository invitationRepository,
    IAuthService authService,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ConfirmAdminInvitationCommand, ConfirmAdminInvitationResult>
{
    private const string AdminRole = "Admin";
    private const int MaxAttempts = 5;

    public async Task<ConfirmAdminInvitationResult> Handle(ConfirmAdminInvitationCommand command, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetPendingByEmailAsync(command.Email, cancellationToken);
        if (invitation is null)
            return new ConfirmAdminInvitationResult(ConfirmAdminInvitationOutcome.InvalidOrExpiredCode, null);

        if (invitation.AttemptCount >= MaxAttempts)
            return new ConfirmAdminInvitationResult(ConfirmAdminInvitationOutcome.TooManyAttempts, null);

        if (!OtpCode.Matches(command.Email, command.Code, invitation.CodeHash))
        {
            invitation.AttemptCount++;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new ConfirmAdminInvitationResult(ConfirmAdminInvitationOutcome.InvalidOrExpiredCode, null);
        }

        if (await authService.FindUserIdByEmailAsync(command.Email, cancellationToken) is not null)
            return new ConfirmAdminInvitationResult(ConfirmAdminInvitationOutcome.EmailAlreadyRegistered, null);

        var registerResult = await authService.RegisterAsync(command.Email, command.Password, emailPreVerified: true, cancellationToken);
        if (registerResult.Auth is null)
            return new ConfirmAdminInvitationResult(ConfirmAdminInvitationOutcome.RegistrationFailed, null);

        await authService.AssignRoleAsync(registerResult.Auth.UserId, AdminRole, cancellationToken);

        invitation.AcceptedAt = DateTimeOffset.UtcNow;

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = invitation.InvitedByUserId,
            Action = "AdminInvitation.Confirmed",
            EntityType = nameof(AdminInvitation),
            EntityId = invitation.Id,
            Details = command.Email,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Re-authenticate, not RegisterAsync's own tokens — those were minted before AssignRoleAsync
        // ran, so they'd carry a stale "User"-only role claim (same reasoning as
        // ConfirmStoreOwnerInvitationCommandHandler).
        var auth = (await authService.LoginAsync(command.Email, command.Password, null, null, cancellationToken)).Auth;

        return new ConfirmAdminInvitationResult(ConfirmAdminInvitationOutcome.Confirmed, auth);
    }
}
