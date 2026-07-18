using Application.Abstractions;
using Application.Common;
using Domain.Inventory;
using Domain.Sales;
using Domain.ValueObjects;

namespace Application.Sales.Commands.ProcessSale;

public sealed class ProcessSaleCommandHandler(
    IStoreRepository storeRepository,
    IProductRepository productRepository,
    IPriceEntryRepository priceEntryRepository,
    ISaleTransactionRepository saleTransactionRepository,
    IStockLevelRepository stockLevelRepository,
    IStockMovementRepository stockMovementRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ProcessSaleCommand, ProcessSaleResult>
{
    private sealed class InsufficientStockSignal(int productId) : Exception
    {
        public int ProductId { get; } = productId;
    }

    public async Task<ProcessSaleResult> Handle(ProcessSaleCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new ProcessSaleResult(ProcessSaleOutcome.StoreNotFound, null, null, null, null);

        if (store.OwnerUserId != command.CashierUserId)
            return new ProcessSaleResult(ProcessSaleOutcome.Forbidden, null, null, null, null);

        var existing = await saleTransactionRepository.GetByIdempotencyKeyAsync(command.StoreId, command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            var existingTotal = existing.Lines.Sum(l => l.UnitPriceAtSale.Amount * l.Quantity);
            return new ProcessSaleResult(ProcessSaleOutcome.Completed, existing.Id, existingTotal, existing.Currency, null);
        }

        var resolvedLines = new List<(int ProductId, int Quantity, Money UnitPrice)>();

        foreach (var line in command.Lines)
        {
            if (!await productRepository.ExistsAsync(line.ProductId, cancellationToken))
                return new ProcessSaleResult(ProcessSaleOutcome.ProductNotFound, null, null, null, line.ProductId);

            var priceEntry = await priceEntryRepository.GetLatestForStoreAsync(line.ProductId, command.StoreId, cancellationToken);
            if (priceEntry is null)
                return new ProcessSaleResult(ProcessSaleOutcome.PriceNotFound, null, null, null, line.ProductId);

            resolvedLines.Add((line.ProductId, line.Quantity, priceEntry.Price));
        }

        SaleTransaction? saleTransaction = null;

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                foreach (var line in resolvedLines)
                {
                    var decremented = await stockLevelRepository.TryDecrementAsync(line.ProductId, command.StoreId, line.Quantity, ct);
                    if (!decremented)
                        throw new InsufficientStockSignal(line.ProductId);
                }

                saleTransaction = new SaleTransaction
                {
                    StoreId = command.StoreId,
                    CashierUserId = command.CashierUserId,
                    IdempotencyKey = command.IdempotencyKey,
                    Currency = command.Currency,
                    Status = SaleStatus.Completed,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                foreach (var line in resolvedLines)
                {
                    saleTransaction.Lines.Add(new SaleLineItem
                    {
                        ProductId = line.ProductId,
                        Quantity = line.Quantity,
                        UnitPriceAtSale = line.UnitPrice
                    });
                }

                saleTransactionRepository.Add(saleTransaction);
                await unitOfWork.SaveChangesAsync(ct);

                foreach (var line in resolvedLines)
                {
                    stockMovementRepository.Add(new StockMovement
                    {
                        ProductId = line.ProductId,
                        StoreId = command.StoreId,
                        Type = StockMovementType.Sale,
                        QuantityDelta = -line.Quantity,
                        RelatedSaleTransactionId = saleTransaction.Id,
                        PerformedByUserId = command.CashierUserId,
                        OccurredAt = DateTimeOffset.UtcNow
                    });
                }

                await unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);
        }
        catch (InsufficientStockSignal signal)
        {
            return new ProcessSaleResult(ProcessSaleOutcome.InsufficientStock, null, null, null, signal.ProductId);
        }

        var total = resolvedLines.Sum(l => l.UnitPrice.Amount * l.Quantity);
        return new ProcessSaleResult(ProcessSaleOutcome.Completed, saleTransaction!.Id, total, command.Currency, null);
    }
}
