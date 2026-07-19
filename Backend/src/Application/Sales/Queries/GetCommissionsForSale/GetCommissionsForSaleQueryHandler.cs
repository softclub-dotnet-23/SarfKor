using Application.Abstractions;
using Application.Common;

namespace Application.Sales.Queries.GetCommissionsForSale;

public sealed class GetCommissionsForSaleQueryHandler(
    ISaleTransactionRepository saleTransactionRepository,
    IStoreRepository storeRepository,
    ICommissionRepository commissionRepository) : IQueryHandler<GetCommissionsForSaleQuery, GetCommissionsForSaleResult>
{
    public async Task<GetCommissionsForSaleResult> Handle(GetCommissionsForSaleQuery query, CancellationToken cancellationToken)
    {
        var sale = await saleTransactionRepository.GetByIdAsync(query.SaleTransactionId, cancellationToken);
        if (sale is null)
            return new GetCommissionsForSaleResult(GetCommissionsForSaleOutcome.SaleNotFound, null);

        var store = await storeRepository.GetByIdAsync(sale.StoreId, cancellationToken);
        if (store is null || store.OwnerUserId != query.RequestedByUserId)
            return new GetCommissionsForSaleResult(GetCommissionsForSaleOutcome.Forbidden, null);

        var commissions = await commissionRepository.GetBySaleTransactionIdAsync(query.SaleTransactionId, cancellationToken);
        var dtos = commissions
            .Select(c => new CommissionDto(c.Id, c.CashierUserId, c.Amount.Amount, c.Amount.Currency, c.CreatedAt))
            .ToList();

        return new GetCommissionsForSaleResult(GetCommissionsForSaleOutcome.Found, dtos);
    }
}
