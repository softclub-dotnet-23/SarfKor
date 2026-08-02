using Domain.Payments;

namespace Application.Abstractions;

public interface IStoreCreditRepository
{
    Task<StoreCredit?> GetByStoreAndCustomerAsync(int storeId, int customerId, CancellationToken cancellationToken);
    void Add(StoreCredit storeCredit);

    /// <summary>Atomically debits the balance only if enough remains. Returns false if insufficient (no write happens).</summary>
    Task<bool> TryDebitAsync(int storeCreditId, decimal amount, CancellationToken cancellationToken);

    /// <summary>Atomically credits (refunds) the balance back onto the account.</summary>
    Task CreditAsync(int storeCreditId, decimal amount, CancellationToken cancellationToken);
}
