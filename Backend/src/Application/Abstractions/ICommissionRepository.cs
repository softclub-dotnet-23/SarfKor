using Domain.Sales;

namespace Application.Abstractions;

public interface ICommissionRepository
{
    Task<IReadOnlyList<Commission>> GetBySaleTransactionIdAsync(int saleTransactionId, CancellationToken cancellationToken);
    void Add(Commission commission);
}
