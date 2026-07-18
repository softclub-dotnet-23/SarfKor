using Application.Abstractions;
using Domain.Receipts;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ReceiptRepository(AppDbContext dbContext) : IReceiptRepository
{
    public Task<Receipt?> GetByIdAsync(int receiptId, CancellationToken cancellationToken) =>
        dbContext.Receipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == receiptId, cancellationToken);

    public void Add(Receipt receipt) => dbContext.Receipts.Add(receipt);
}
