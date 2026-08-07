using Application.Abstractions;
using Application.Common;
using Domain.Auditing;

namespace Application.Identity.Commands.BlockUser;

public sealed class BlockUserCommandHandler(
    IAuthService authService,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<BlockUserCommand, BlockUserResult>
{
    public async Task<BlockUserResult> Handle(BlockUserCommand command, CancellationToken cancellationToken)
    {
        var blocked = await authService.BlockUserAsync(command.UserId, command.Reason, command.PerformedByAdminUserId, cancellationToken);
        if (!blocked)
            return new BlockUserResult(BlockUserOutcome.NotFound);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByAdminUserId,
            Action = "User.Blocked",
            EntityType = "ApplicationUser",
            EntityId = 0,
            Reason = command.Reason,
            Details = command.UserId,
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BlockUserResult(BlockUserOutcome.Blocked);
    }
}
