using Application.Abstractions;
using Domain.Catalog;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class TaxRateRepository(AppDbContext dbContext) : ITaxRateRepository
{
    public async Task<IReadOnlyList<TaxRate>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.TaxRates.ToListAsync(cancellationToken);

    public void Add(TaxRate taxRate) => dbContext.TaxRates.Add(taxRate);
}
