using Application.Abstractions;
using Application.Common;
using Domain.Catalog;
using Domain.ValueObjects;

namespace Application.Catalog.Commands.CreateProductBundle;

public sealed class CreateProductBundleCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IProductRepository productRepository,
    IProductBundleRepository productBundleRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateProductBundleCommand, CreateProductBundleResult>
{
    public async Task<CreateProductBundleResult> Handle(CreateProductBundleCommand command, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(command.StoreId, cancellationToken))
            return new CreateProductBundleResult(CreateProductBundleOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
            return new CreateProductBundleResult(CreateProductBundleOutcome.Forbidden, null);

        if (!await storeAccessAuthorizer.IsOperationalAsync(command.StoreId, cancellationToken))
            return new CreateProductBundleResult(CreateProductBundleOutcome.SubscriptionInactive, null);

        // Batched existence check — without this, a bad ProductId only surfaces as a raw
        // DbUpdateException from the FK constraint when SaveChangesAsync runs.
        var requestedProductIds = command.Items.Select(i => i.ProductId).Distinct().ToList();
        var foundProducts = await productRepository.GetByIdsAsync(requestedProductIds, cancellationToken);
        if (foundProducts.Count != requestedProductIds.Count)
            return new CreateProductBundleResult(CreateProductBundleOutcome.ProductNotFound, null);

        var bundle = new ProductBundle
        {
            StoreId = command.StoreId,
            Name = command.Name,
            BundlePrice = new Money(command.BundlePrice, command.Currency),
            Items = command.Items.Select(i => new ProductBundleItem { ProductId = i.ProductId, Quantity = i.Quantity }).ToList()
        };

        productBundleRepository.Add(bundle);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateProductBundleResult(CreateProductBundleOutcome.Created, bundle.Id);
    }
}
