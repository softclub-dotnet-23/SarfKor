using Application.Abstractions;
using Application.Common;
using Application.Subscriptions;
using Domain.Auditing;
using Domain.Stores;
using Domain.Subscriptions;
using Microsoft.Extensions.Options;

namespace Application.Stores.Commands.ApproveStore;

public sealed class ApproveStoreCommandHandler(
    IStoreRepository storeRepository,
    ISubscriptionPlanRepository subscriptionPlanRepository,
    IStoreSubscriptionRepository storeSubscriptionRepository,
    IAuditLogRepository auditLogRepository,
    IOptions<SubscriptionOptions> subscriptionOptions,
    IUnitOfWork unitOfWork) : ICommandHandler<ApproveStoreCommand, ApproveStoreResult>
{
    public async Task<ApproveStoreResult> Handle(ApproveStoreCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new ApproveStoreResult(ApproveStoreOutcome.NotFound);

        if (store.Status == StoreStatus.Active)
            return new ApproveStoreResult(ApproveStoreOutcome.AlreadyApproved);

        // No IStoreAccessAuthorizer check here on purpose — this is an Admin action, not an
        // ownership check, and an Admin approving their own store would defeat the point of it.
        var previousStatus = store.Status;
        store.Status = StoreStatus.Active;
        store.StatusReason = null;
        store.StatusChangedAt = DateTimeOffset.UtcNow;

        // ADMIN_PROMPT.md §2.1: approval always starts a Trial subscription — Trial:DurationDays
        // defaults to 14, matching the doc's default. A default plan (SubscriptionPlan:DefaultCode,
        // falling back to the cheapest active plan) prices it; if no plan exists at all yet (a fresh
        // database before an Admin has created one), the store is still approved but left without a
        // subscription row — IStoreAccessAuthorizer.IsOperationalAsync treats "no subscription" as
        // operational, so this never locks a store out over an Admin's setup order.
        var options = subscriptionOptions.Value;
        var trialDays = options.TrialDurationDays;
        var defaultPlanCode = options.DefaultPlanCode;
        var plan = defaultPlanCode is not null
            ? await subscriptionPlanRepository.GetByCodeAsync(defaultPlanCode, cancellationToken)
            : null;
        plan ??= (await subscriptionPlanRepository.GetAllAsync(includeInactive: false, cancellationToken)).FirstOrDefault();

        if (plan is not null && await storeSubscriptionRepository.GetByStoreIdAsync(store.Id, cancellationToken) is null)
        {
            storeSubscriptionRepository.Add(new StoreSubscription
            {
                StoreId = store.Id,
                SubscriptionPlanId = plan.Id,
                Status = SubscriptionStatus.Trial,
                StartedAt = DateTimeOffset.UtcNow,
                CurrentPeriodEndsAt = DateTimeOffset.UtcNow.AddDays(trialDays),
                PriceAtIssue = plan.MonthlyPrice
            });
        }

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "Store.Approved",
            EntityType = nameof(Store),
            EntityId = store.Id,
            IpAddress = command.PerformedByIpAddress,
            BeforeStateJson = $$"""{"status":"{{previousStatus}}"}""",
            AfterStateJson = $$"""{"status":"Active"}""",
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ApproveStoreResult(ApproveStoreOutcome.Approved);
    }
}
