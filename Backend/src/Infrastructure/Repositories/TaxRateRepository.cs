using Application.Abstractions;
using Domain.Catalog;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class TaxRateRepository(AppDbContext dbContext) : ITaxRateRepository
{
    public async Task<IReadOnlyList<TaxRate>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.TaxRates.ToListAsync(cancellationToken);

    public Task<TaxRate?> GetByIdAsync(int taxRateId, CancellationToken cancellationToken) =>
        dbContext.TaxRates.FirstOrDefaultAsync(t => t.Id == taxRateId, cancellationToken);

    public Task<bool> IsInUseAsync(int taxRateId, CancellationToken cancellationToken) =>
        dbContext.Products.AnyAsync(p => p.TaxRateId == taxRateId, cancellationToken);

    public void Add(TaxRate taxRate) => dbContext.TaxRates.Add(taxRate);

    public void Remove(TaxRate taxRate) => dbContext.TaxRates.Remove(taxRate);
}
