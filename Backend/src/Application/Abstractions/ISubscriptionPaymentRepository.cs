using Domain.Subscriptions;

namespace Application.Abstractions;

public interface ISubscriptionPaymentRepository
{
    Task<SubscriptionPayment?> GetByIdAsync(int subscriptionPaymentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SubscriptionPayment>> GetByStoreSubscriptionIdAsync(int storeSubscriptionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SubscriptionPayment>> GetAllAsync(
        int skip, int take, int? storeId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);

    Task<int> CountAllAsync(int? storeId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);

    void Add(SubscriptionPayment payment);
}
