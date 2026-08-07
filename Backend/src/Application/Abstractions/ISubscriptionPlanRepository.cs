using Domain.Subscriptions;

namespace Application.Abstractions;

public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetByIdAsync(int subscriptionPlanId, CancellationToken cancellationToken);
    Task<SubscriptionPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task<IReadOnlyList<SubscriptionPlan>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken);
    void Add(SubscriptionPlan plan);
}
