using Domain.Catalog;

namespace Application.Abstractions;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int categoryId, CancellationToken cancellationToken);
    void Add(Category category);
}
