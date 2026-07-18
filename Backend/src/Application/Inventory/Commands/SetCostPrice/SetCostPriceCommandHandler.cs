using Application.Abstractions;
using Application.Common;
using Domain.Inventory;
using Domain.ValueObjects;

namespace Application.Inventory.Commands.SetCostPrice;

public sealed class SetCostPriceCommandHandler(
    IStoreRepository storeRepository,
    IProductRepository productRepository,
    ICostPriceRepository costPriceRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<SetCostPriceCommand, SetCostPriceResult>
{
    public async Task<SetCostPriceResult> Handle(SetCostPriceCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new SetCostPriceResult(SetCostPriceOutcome.StoreNotFound, null);

        if (store.OwnerUserId != command.PerformedByUserId)
            return new SetCostPriceResult(SetCostPriceOutcome.Forbidden, null);

        if (!await productRepository.ExistsAsync(command.ProductId, cancellationToken))
            return new SetCostPriceResult(SetCostPriceOutcome.ProductNotFound, null);

        // Append-only history (same pattern as PriceEntry) — GetLatestForStoreAsync picks the newest row per product.
        var costPrice = new CostPrice
        {
            ProductId = command.ProductId,
            StoreId = command.StoreId,
            Amount = new Money(command.Amount, command.Currency),
            SetByUserId = command.PerformedByUserId,
            EffectiveFrom = DateTimeOffset.UtcNow
        };

        costPriceRepository.Add(costPrice);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SetCostPriceResult(SetCostPriceOutcome.Set, costPrice.Id);
    }
}
