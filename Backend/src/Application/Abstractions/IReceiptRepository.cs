using Domain.Receipts;

namespace Application.Abstractions;

public interface IReceiptRepository
{
    Task<Receipt?> GetByIdAsync(int receiptId, CancellationToken cancellationToken);
}
