using Domain.Catalog;

namespace Application.Abstractions;

public interface IBrandRepository
{
    Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken cancellationToken);
    void Add(Brand brand);
}
