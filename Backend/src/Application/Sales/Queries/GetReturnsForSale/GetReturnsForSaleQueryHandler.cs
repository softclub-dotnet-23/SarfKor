using Application.Abstractions;
using Application.Common;

namespace Application.Sales.Queries.GetReturnsForSale;

public sealed class GetReturnsForSaleQueryHandler(
    ISaleTransactionRepository saleTransactionRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    ISaleReturnRepository saleReturnRepository) : IQueryHandler<GetReturnsForSaleQuery, GetReturnsForSaleResult>
{
    public async Task<GetReturnsForSaleResult> Handle(GetReturnsForSaleQuery query, CancellationToken cancellationToken)
    {
        var sale = await saleTransactionRepository.GetByIdAsync(query.SaleTransactionId, cancellationToken);
        if (sale is null)
            return new GetReturnsForSaleResult(GetReturnsForSaleOutcome.SaleNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(sale.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetReturnsForSaleResult(GetReturnsForSaleOutcome.Forbidden, null);

        var returns = await saleReturnRepository.GetBySaleTransactionIdAsync(query.SaleTransactionId, cancellationToken);
        var dtos = returns
            .Select(r => new SaleReturnDto(
                r.Id,
                r.Reason,
                r.CreatedAt,
                r.Lines.Select(l => new ReturnLineDto(l.SaleLineItemId, l.Quantity, l.RefundAmount.Amount)).ToList()))
            .ToList();

        return new GetReturnsForSaleResult(GetReturnsForSaleOutcome.Found, dtos);
    }
}
