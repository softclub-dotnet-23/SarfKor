using Application.Abstractions;
using Domain.Catalog;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CategoryRepository(AppDbContext dbContext) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Categories.ToListAsync(cancellationToken);

    public Task<Category?> GetByIdAsync(int categoryId, CancellationToken cancellationToken) =>
        dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

    public Task<bool> ExistsAsync(int categoryId, CancellationToken cancellationToken) =>
        dbContext.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken);

    // Category is weakly referenced (plain int columns, no DB-level FK) by Product.CategoryId,
    // TaxRate.CategoryId, and other Category rows via ParentCategoryId — deleting one out from
    // under any of those would silently orphan them, so callers must check this first.
    public async Task<bool> IsInUseAsync(int categoryId, CancellationToken cancellationToken)
    {
        if (await dbContext.Products.AnyAsync(p => p.CategoryId == categoryId, cancellationToken))
            return true;
        if (await dbContext.TaxRates.AnyAsync(t => t.CategoryId == categoryId, cancellationToken))
            return true;
        return await dbContext.Categories.AnyAsync(c => c.ParentCategoryId == categoryId, cancellationToken);
    }

    public void Add(Category category) => dbContext.Categories.Add(category);

    public void Remove(Category category) => dbContext.Categories.Remove(category);
}
