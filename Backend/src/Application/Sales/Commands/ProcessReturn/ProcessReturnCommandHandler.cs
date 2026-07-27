using Application.Abstractions;
using Application.Common;
using Domain.Inventory;
using Domain.Sales;

namespace Application.Sales.Commands.ProcessReturn;

public sealed class ProcessReturnCommandHandler(
    ISaleTransactionRepository saleTransactionRepository,
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository,
    ISaleReturnRepository saleReturnRepository,
    IStockLevelRepository stockLevelRepository,
    IStockMovementRepository stockMovementRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ProcessReturnCommand, ProcessReturnResult>
{
    public async Task<ProcessReturnResult> Handle(ProcessReturnCommand command, CancellationToken cancellationToken)
    {
        var sale = await saleTransactionRepository.GetByIdAsync(command.SaleTransactionId, cancellationToken);
        if (sale is null)
            return new ProcessReturnResult(ProcessReturnOutcome.SaleNotFound, null, null, null);

        var store = await storeRepository.GetByIdAsync(sale.StoreId, cancellationToken);
        if (store is null)
            return new ProcessReturnResult(ProcessReturnOutcome.Forbidden, null, null, null);

        if (store.OwnerUserId != command.PerformedByUserId
            && !await storeEmployeeRepository.IsEmployeeAsync(store.Id, command.PerformedByUserId, cancellationToken))
            return new ProcessReturnResult(ProcessReturnOutcome.Forbidden, null, null, null);

        if (sale.Status != SaleStatus.Completed)
            return new ProcessReturnResult(ProcessReturnOutcome.SaleNotCompleted, null, null, null);

        // A line can be returned across multiple partial returns — sum what's already gone before
        // allowing more, so the same units can't be refunded twice.
        var priorReturns = await saleReturnRepository.GetBySaleTransactionIdAsync(command.SaleTransactionId, cancellationToken);
        var alreadyReturnedByLine = priorReturns
            .SelectMany(r => r.Lines)
            .GroupBy(l => l.SaleLineItemId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        var returnLines = new List<ReturnLineItem>();
        decimal totalRefund = 0;

        foreach (var input in command.Lines)
        {
            var saleLine = sale.Lines.FirstOrDefault(l => l.Id == input.SaleLineItemId);
            if (saleLine is null)
                return new ProcessReturnResult(ProcessReturnOutcome.LineNotFound, null, null, input.SaleLineItemId);

            var alreadyReturned = alreadyReturnedByLine.GetValueOrDefault(input.SaleLineItemId, 0);
            var available = saleLine.Quantity - alreadyReturned;
            if (input.Quantity > available)
                return new ProcessReturnResult(ProcessReturnOutcome.ExceedsAvailableQuantity, null, null, input.SaleLineItemId);

            var refundAmount = saleLine.UnitPriceAtSale with { Amount = saleLine.UnitPriceAtSale.Amount * input.Quantity };
            totalRefund += refundAmount.Amount;

            returnLines.Add(new ReturnLineItem
            {
                SaleLineItemId = input.SaleLineItemId,
                Quantity = input.Quantity,
                RefundAmount = refundAmount
            });
        }

        var saleReturn = new SaleReturn
        {
            SaleTransactionId = command.SaleTransactionId,
            ProcessedByUserId = command.PerformedByUserId,
            Reason = command.Reason,
            CreatedAt = DateTimeOffset.UtcNow,
            Lines = returnLines
        };

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            foreach (var input in command.Lines)
            {
                var saleLine = sale.Lines.First(l => l.Id == input.SaleLineItemId);

                await stockLevelRepository.IncrementAsync(saleLine.ProductId, sale.StoreId, input.Quantity, ct);
                stockMovementRepository.Add(new StockMovement
                {
                    ProductId = saleLine.ProductId,
                    StoreId = sale.StoreId,
                    Type = StockMovementType.Correction,
                    QuantityDelta = input.Quantity,
                    Reason = $"Return from sale {sale.Id}: {command.Reason}",
                    RelatedSaleTransactionId = sale.Id,
                    PerformedByUserId = command.PerformedByUserId,
                    OccurredAt = DateTimeOffset.UtcNow
                });
            }

            saleReturnRepository.Add(saleReturn);
            await unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        return new ProcessReturnResult(ProcessReturnOutcome.Processed, saleReturn.Id, totalRefund, null);
    }
}
