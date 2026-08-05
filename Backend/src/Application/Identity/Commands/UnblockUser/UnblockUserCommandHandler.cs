using Application.Abstractions;
using Application.Common;
using Domain.Auditing;

namespace Application.Identity.Commands.UnblockUser;

public sealed class UnblockUserCommandHandler(
    IAuthService authService,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UnblockUserCommand, UnblockUserResult>
{
    public async Task<UnblockUserResult> Handle(UnblockUserCommand command, CancellationToken cancellationToken)
    {
        var unblocked = await authService.UnblockUserAsync(command.UserId, cancellationToken);
        if (!unblocked)
            return new UnblockUserResult(UnblockUserOutcome.NotFound);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByAdminUserId,
            Action = "User.Unblocked",
            EntityType = "ApplicationUser",
            EntityId = 0,
            Reason = command.Reason,
            Details = command.UserId,
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UnblockUserResult(UnblockUserOutcome.Unblocked);
    }
}
