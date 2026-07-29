using Application.Abstractions;
using Application.Common;

namespace Application.Stores.Queries.GetMyStores;

public sealed class GetMyStoresQueryHandler(
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository) : IQueryHandler<GetMyStoresQuery, GetMyStoresResult>
{
    public async Task<GetMyStoresResult> Handle(GetMyStoresQuery query, CancellationToken cancellationToken)
    {
        var owned = await storeRepository.GetOwnedByUserIdAsync(query.UserId, cancellationToken);
        var dtos = owned.Select(s => new MyStoreDto(s.Id, s.Name, "Owner")).ToList();

        var ownedIds = owned.Select(s => s.Id).ToHashSet();
        var employments = await storeEmployeeRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var roleByStoreId = employments
            .Where(e => !ownedIds.Contains(e.StoreId))
            .ToDictionary(e => e.StoreId, e => e.Role.ToString());
        if (roleByStoreId.Count > 0)
        {
            var employedStores = await storeRepository.GetByIdsAsync(roleByStoreId.Keys, cancellationToken);
            dtos.AddRange(employedStores.Select(s => new MyStoreDto(s.Id, s.Name, roleByStoreId[s.Id])));
        }

        return new GetMyStoresResult(dtos);
    }
}
