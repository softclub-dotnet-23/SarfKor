using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Identity.Commands.InviteAdmin;

public sealed class InviteAdminCommandHandler(
    IAdminInvitationRepository invitationRepository,
    IAuthService authService,
    IEmailSender emailSender,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<InviteAdminCommandHandler> logger) : ICommandHandler<InviteAdminCommand, InviteAdminResult>
{
    private static readonly TimeSpan InvitationLifespan = TimeSpan.FromMinutes(20);

    public async Task<InviteAdminResult> Handle(InviteAdminCommand command, CancellationToken cancellationToken)
    {
        if (await authService.FindUserIdByEmailAsync(command.Email, cancellationToken) is not null)
            return new InviteAdminResult(InviteAdminOutcome.EmailAlreadyRegistered, null);

        var code = OtpCode.Generate();
        var codeHash = OtpCode.Hash(command.Email, code);

        var invitation = await invitationRepository.GetPendingByEmailAsync(command.Email, cancellationToken);
        if (invitation is null)
        {
            invitation = new AdminInvitation
            {
                Email = command.Email,
                CodeHash = codeHash,
                InvitedByUserId = command.InvitedByAdminUserId,
                ExpiresAt = DateTimeOffset.UtcNow.Add(InvitationLifespan),
                CreatedAt = DateTimeOffset.UtcNow
            };
            invitationRepository.Add(invitation);
        }
        else
        {
            invitation.CodeHash = codeHash;
            invitation.AttemptCount = 0;
            invitation.ExpiresAt = DateTimeOffset.UtcNow.Add(InvitationLifespan);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.InvitedByAdminUserId,
            Action = "AdminInvitation.Created",
            EntityType = nameof(AdminInvitation),
            EntityId = invitation.Id,
            Details = command.Email,
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendAdminInvitationEmailAsync(command.Email, code, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send admin invitation email");
        }

        return new InviteAdminResult(InviteAdminOutcome.Invited, invitation.Id);
    }
}
