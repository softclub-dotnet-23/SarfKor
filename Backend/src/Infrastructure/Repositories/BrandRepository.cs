using Application.Abstractions;
using Domain.Catalog;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class BrandRepository(AppDbContext dbContext) : IBrandRepository
{
    public async Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Brands.ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(int brandId, CancellationToken cancellationToken) =>
        dbContext.Brands.AnyAsync(b => b.Id == brandId, cancellationToken);

    public void Add(Brand brand) => dbContext.Brands.Add(brand);
}
