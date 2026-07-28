using Domain.Catalog;

namespace Application.Abstractions;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken);
    Task<Category?> GetByIdAsync(int categoryId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int categoryId, CancellationToken cancellationToken);
    Task<bool> IsInUseAsync(int categoryId, CancellationToken cancellationToken);
    void Add(Category category);
    void Remove(Category category);
}
