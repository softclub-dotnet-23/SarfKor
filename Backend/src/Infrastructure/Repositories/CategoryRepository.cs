using Application.Abstractions;
using Domain.Catalog;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CategoryRepository(AppDbContext dbContext) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Categories.ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(int categoryId, CancellationToken cancellationToken) =>
        dbContext.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken);

    public void Add(Category category) => dbContext.Categories.Add(category);
}
