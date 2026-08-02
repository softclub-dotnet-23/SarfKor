using Application.Abstractions;
using Application.Common;
using Domain.Inventory;
using Domain.ValueObjects;

namespace Application.Inventory.Commands.SetCostPrice;

public sealed class SetCostPriceCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IProductRepository productRepository,
    ICostPriceRepository costPriceRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<SetCostPriceCommand, SetCostPriceResult>
{
    public async Task<SetCostPriceResult> Handle(SetCostPriceCommand command, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(command.StoreId, cancellationToken))
            return new SetCostPriceResult(SetCostPriceOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
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
