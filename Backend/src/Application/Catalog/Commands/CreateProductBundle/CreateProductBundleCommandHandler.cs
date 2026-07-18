using Application.Abstractions;
using Application.Common;
using Domain.Catalog;
using Domain.ValueObjects;

namespace Application.Catalog.Commands.CreateProductBundle;

public sealed class CreateProductBundleCommandHandler(
    IStoreRepository storeRepository,
    IProductBundleRepository productBundleRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateProductBundleCommand, CreateProductBundleResult>
{
    public async Task<CreateProductBundleResult> Handle(CreateProductBundleCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new CreateProductBundleResult(CreateProductBundleOutcome.StoreNotFound, null);

        if (store.OwnerUserId != command.PerformedByUserId)
            return new CreateProductBundleResult(CreateProductBundleOutcome.Forbidden, null);

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
