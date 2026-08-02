using Application.Abstractions;
using Domain.Pricing;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PriceEntryDisputeRepository(AppDbContext dbContext) : IPriceEntryDisputeRepository
{
    public Task<PriceEntryDispute?> GetByIdAsync(int priceEntryDisputeId, CancellationToken cancellationToken) =>
        dbContext.PriceEntryDisputes.FirstOrDefaultAsync(d => d.Id == priceEntryDisputeId, cancellationToken);

    public async Task<IReadOnlyList<PriceEntryDispute>> GetPendingAsync(CancellationToken cancellationToken) =>
        await dbContext.PriceEntryDisputes.Where(d => d.Status == PriceEntryDisputeStatus.Pending).ToListAsync(cancellationToken);

    public Task<bool> HasPendingDisputeAsync(int priceEntryId, CancellationToken cancellationToken) =>
        dbContext.PriceEntryDisputes.AnyAsync(d => d.PriceEntryId == priceEntryId && d.Status == PriceEntryDisputeStatus.Pending, cancellationToken);

    public void Add(PriceEntryDispute dispute) => dbContext.PriceEntryDisputes.Add(dispute);
}
