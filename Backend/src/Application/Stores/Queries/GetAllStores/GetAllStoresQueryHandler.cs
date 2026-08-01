using Application.Abstractions;
using Application.Common;

namespace Application.Stores.Queries.GetAllStores;

public sealed class GetAllStoresQueryHandler(
    IStoreRepository storeRepository,
    IAuthService authService) : IQueryHandler<GetAllStoresQuery, GetAllStoresResult>
{
    public async Task<GetAllStoresResult> Handle(GetAllStoresQuery query, CancellationToken cancellationToken)
    {
        var stores = await storeRepository.GetAllAsync(query.Skip, query.Take, cancellationToken);
        var totalCount = await storeRepository.CountAllAsync(cancellationToken);

        // One batched lookup for the page's owners instead of one query per store.
        var ownerIds = stores.Select(s => s.OwnerUserId).Distinct().ToList();
        var emailsByOwnerId = await authService.GetEmailsByUserIdsAsync(ownerIds, cancellationToken);

        var dtos = stores
            .Select(s => new AdminStoreDto(
                s.Id,
                s.Name,
                s.Address,
                s.Location.Latitude,
                s.Location.Longitude,
                s.Status,
                s.OwnerUserId,
                emailsByOwnerId.GetValueOrDefault(s.OwnerUserId)))
            .ToList();

        return new GetAllStoresResult(dtos, totalCount);
    }
}
