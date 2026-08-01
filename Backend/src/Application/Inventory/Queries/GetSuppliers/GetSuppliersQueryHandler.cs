using Application.Abstractions;
using Application.Common;

namespace Application.Inventory.Queries.GetSuppliers;

public sealed class GetSuppliersQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    ISupplierRepository supplierRepository) : IQueryHandler<GetSuppliersQuery, GetSuppliersResult>
{
    public async Task<GetSuppliersResult> Handle(GetSuppliersQuery query, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(query.StoreId, cancellationToken))
            return new GetSuppliersResult(GetSuppliersOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(query.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetSuppliersResult(GetSuppliersOutcome.Forbidden, null);

        var suppliers = await supplierRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        var dtos = suppliers.Select(s => new SupplierDto(s.Id, s.Name, s.ContactPhone, s.ContactEmail)).ToList();
        return new GetSuppliersResult(GetSuppliersOutcome.Found, dtos);
    }
}
