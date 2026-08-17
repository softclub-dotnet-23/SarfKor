using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Stores;

namespace Application.Stores.Commands.SetStoreEmployeeActive;

public sealed class SetStoreEmployeeActiveCommandHandler(
    IStoreEmployeeRepository storeEmployeeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IRefreshTokenRepository refreshTokenRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<SetStoreEmployeeActiveCommand, SetStoreEmployeeActiveResult>
{
    public async Task<SetStoreEmployeeActiveResult> Handle(SetStoreEmployeeActiveCommand command, CancellationToken cancellationToken)
    {
        var employee = await storeEmployeeRepository.GetByIdAsync(command.StoreEmployeeId, cancellationToken);
        if (employee is null)
            return new SetStoreEmployeeActiveResult(SetStoreEmployeeActiveOutcome.NotFound);

        if (!await storeAccessAuthorizer.IsOwnerAsync(employee.StoreId, command.PerformedByUserId, cancellationToken))
            return new SetStoreEmployeeActiveResult(SetStoreEmployeeActiveOutcome.Forbidden);

        // Code review 2026-08-10 finding #7: nothing stopped an owner from disabling their OWN
        // employee row via a direct API call (the frontend hides the button for isSelf, but per
        // CLAUDE.md that's UX, not enforcement) -- self-lockout with no in-app recovery path.
        if (employee.UserId == command.PerformedByUserId)
            return new SetStoreEmployeeActiveResult(SetStoreEmployeeActiveOutcome.CannotDisableSelf);

        if (!await storeAccessAuthorizer.IsOperationalAsync(employee.StoreId, cancellationToken))
            return new SetStoreEmployeeActiveResult(SetStoreEmployeeActiveOutcome.SubscriptionInactive);

        employee.IsActive = command.IsActive;
        storeEmployeeRepository.Update(employee);

        // Code review 2026-08-10 finding #6: every other password/security-sensitive mutation in
        // this codebase (ChangePasswordAsync, ResetPasswordAsync, AdminResetPasswordAsync) revokes
        // refresh tokens "the moment an existing session should die" -- disabling a cashier is
        // exactly that moment too, and relying solely on every future endpoint re-checking IsActive
        // live is defense-in-depth's whole point, not a substitute for it.
        if (!command.IsActive)
            await refreshTokenRepository.RevokeAllForUserAsync(employee.UserId, cancellationToken);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = command.IsActive ? "CashierAccount.Enabled" : "CashierAccount.Disabled",
            EntityType = nameof(StoreEmployee),
            EntityId = employee.Id,
            Details = $"{(command.IsActive ? "Enabled" : "Disabled")} {employee.FirstName} {employee.LastName} (store {employee.StoreId})",
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow,
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SetStoreEmployeeActiveResult(SetStoreEmployeeActiveOutcome.Updated);
    }
}
