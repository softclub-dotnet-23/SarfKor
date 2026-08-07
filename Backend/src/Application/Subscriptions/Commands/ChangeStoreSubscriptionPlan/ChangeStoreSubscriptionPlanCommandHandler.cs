using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Subscriptions;

namespace Application.Subscriptions.Commands.ChangeStoreSubscriptionPlan;

// "Смена тарифа: пересчёта задним числом нет, новый тариф действует со следующего периода"
// (ADMIN_PROMPT.md §2.1) — SubscriptionPlanId changes now (so the next RecordSubscriptionPaymentCommand
// bills at the new plan's terms) but CurrentPeriodEndsAt is untouched, so the already-paid-for
// current period is unaffected.
public sealed class ChangeStoreSubscriptionPlanCommandHandler(
    IStoreSubscriptionRepository storeSubscriptionRepository,
    ISubscriptionPlanRepository subscriptionPlanRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ChangeStoreSubscriptionPlanCommand, ChangeStoreSubscriptionPlanResult>
{
    public async Task<ChangeStoreSubscriptionPlanResult> Handle(ChangeStoreSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        var subscription = await storeSubscriptionRepository.GetByIdAsync(command.StoreSubscriptionId, cancellationToken);
        if (subscription is null)
            return new ChangeStoreSubscriptionPlanResult(ChangeStoreSubscriptionPlanOutcome.SubscriptionNotFound);

        var newPlan = await subscriptionPlanRepository.GetByIdAsync(command.NewSubscriptionPlanId, cancellationToken);
        if (newPlan is null)
            return new ChangeStoreSubscriptionPlanResult(ChangeStoreSubscriptionPlanOutcome.PlanNotFound);

        var previousPlanId = subscription.SubscriptionPlanId;
        subscription.SubscriptionPlanId = newPlan.Id;

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "StoreSubscription.PlanChanged",
            EntityType = nameof(StoreSubscription),
            EntityId = subscription.Id,
            IpAddress = command.PerformedByIpAddress,
            BeforeStateJson = $$"""{"subscriptionPlanId":{{previousPlanId}}}""",
            AfterStateJson = $$"""{"subscriptionPlanId":{{newPlan.Id}}}""",
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ChangeStoreSubscriptionPlanResult(ChangeStoreSubscriptionPlanOutcome.Changed);
    }
}
