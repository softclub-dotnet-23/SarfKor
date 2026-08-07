using Application.Abstractions;
using Domain.Subscriptions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class SubscriptionPaymentRepository(AppDbContext dbContext) : ISubscriptionPaymentRepository
{
    public Task<SubscriptionPayment?> GetByIdAsync(int subscriptionPaymentId, CancellationToken cancellationToken) =>
        dbContext.SubscriptionPayments.FirstOrDefaultAsync(p => p.Id == subscriptionPaymentId, cancellationToken);

    public async Task<IReadOnlyList<SubscriptionPayment>> GetByStoreSubscriptionIdAsync(int storeSubscriptionId, CancellationToken cancellationToken) =>
        await dbContext.SubscriptionPayments
            .Where(p => p.StoreSubscriptionId == storeSubscriptionId)
            .OrderByDescending(p => p.RecordedAt)
            .ToListAsync(cancellationToken);

    private IQueryable<SubscriptionPayment> ApplyFilter(int? storeId, DateOnly? from, DateOnly? to)
    {
        var query = dbContext.SubscriptionPayments.AsQueryable();
        if (storeId is not null)
            query = query.Where(p => dbContext.StoreSubscriptions.Any(s => s.Id == p.StoreSubscriptionId && s.StoreId == storeId));
        if (from is not null) query = query.Where(p => p.PeriodEnd >= from);
        if (to is not null) query = query.Where(p => p.PeriodStart <= to);
        return query;
    }

    public async Task<IReadOnlyList<SubscriptionPayment>> GetAllAsync(int skip, int take, int? storeId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken) =>
        await ApplyFilter(storeId, from, to).OrderByDescending(p => p.RecordedAt).Skip(skip).Take(take).ToListAsync(cancellationToken);

    public Task<int> CountAllAsync(int? storeId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken) =>
        ApplyFilter(storeId, from, to).CountAsync(cancellationToken);

    public void Add(SubscriptionPayment payment) => dbContext.SubscriptionPayments.Add(payment);
}
