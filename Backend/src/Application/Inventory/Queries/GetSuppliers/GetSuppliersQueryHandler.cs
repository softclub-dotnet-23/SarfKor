using Application.Abstractions;
using Application.Common;

namespace Application.Inventory.Queries.GetSuppliers;

public sealed class GetSuppliersQueryHandler(ISupplierRepository supplierRepository) : IQueryHandler<GetSuppliersQuery, GetSuppliersResult>
{
    public async Task<GetSuppliersResult> Handle(GetSuppliersQuery query, CancellationToken cancellationToken)
    {
        var suppliers = await supplierRepository.GetAllAsync(cancellationToken);
        return new GetSuppliersResult(suppliers.Select(s => new SupplierDto(s.Id, s.Name, s.ContactPhone, s.ContactEmail)).ToList());
    }
}
