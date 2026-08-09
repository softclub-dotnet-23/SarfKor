using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Stores;

namespace Application.Stores.Commands.ResetCashierPassword;

public sealed class ResetCashierPasswordCommandHandler(
    IStoreEmployeeRepository storeEmployeeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IAuthService authService,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ResetCashierPasswordCommand, ResetCashierPasswordResult>
{
    public async Task<ResetCashierPasswordResult> Handle(ResetCashierPasswordCommand command, CancellationToken cancellationToken)
    {
        var employee = await storeEmployeeRepository.GetByIdAsync(command.StoreEmployeeId, cancellationToken);
        if (employee is null)
            return new ResetCashierPasswordResult(ResetCashierPasswordOutcome.NotFound);

        if (!await storeAccessAuthorizer.IsOwnerAsync(employee.StoreId, command.PerformedByUserId, cancellationToken))
            return new ResetCashierPasswordResult(ResetCashierPasswordOutcome.Forbidden);

        var newPassword = GeneratedPassword.Generate();
        var succeeded = await authService.AdminResetPasswordAsync(employee.UserId, newPassword, cancellationToken);
        if (!succeeded)
            return new ResetCashierPasswordResult(ResetCashierPasswordOutcome.NotFound);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "CashierAccount.PasswordReset",
            EntityType = nameof(StoreEmployee),
            EntityId = employee.Id,
            Details = $"Reset password for {employee.FirstName} {employee.LastName} (store {employee.StoreId})",
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow,
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ResetCashierPasswordResult(ResetCashierPasswordOutcome.Reset, newPassword);
    }
}
