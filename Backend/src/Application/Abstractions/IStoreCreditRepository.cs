using Domain.Payments;

namespace Application.Abstractions;

public interface IStoreCreditRepository
{
    Task<StoreCredit?> GetByStoreAndCustomerAsync(int storeId, int customerId, CancellationToken cancellationToken);
    void Add(StoreCredit storeCredit);
}
