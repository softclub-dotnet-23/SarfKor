using Application.Abstractions;
using Domain.Subscriptions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class SubscriptionPlanRepository(AppDbContext dbContext) : ISubscriptionPlanRepository
{
    public Task<SubscriptionPlan?> GetByIdAsync(int subscriptionPlanId, CancellationToken cancellationToken) =>
        dbContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == subscriptionPlanId, cancellationToken);

    public Task<SubscriptionPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);

    public async Task<IReadOnlyList<SubscriptionPlan>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = dbContext.SubscriptionPlans.AsQueryable();
        if (!includeInactive) query = query.Where(p => p.IsActive);
        return await query.OrderBy(p => p.MonthlyPrice.Amount).ToListAsync(cancellationToken);
    }

    public void Add(SubscriptionPlan plan) => dbContext.SubscriptionPlans.Add(plan);
}
