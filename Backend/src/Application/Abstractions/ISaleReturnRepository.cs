using Domain.Sales;

namespace Application.Abstractions;

public interface ISaleReturnRepository
{
    Task<IReadOnlyList<SaleReturn>> GetBySaleTransactionIdAsync(int saleTransactionId, CancellationToken cancellationToken);
    void Add(SaleReturn saleReturn);
}
