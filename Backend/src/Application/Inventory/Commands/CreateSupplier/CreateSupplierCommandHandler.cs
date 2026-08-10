using Application.Abstractions;
using Application.Common;
using Domain.Inventory;

namespace Application.Inventory.Commands.CreateSupplier;

public sealed class CreateSupplierCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    ISupplierRepository supplierRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateSupplierCommand, CreateSupplierResult>
{
    public async Task<CreateSupplierResult> Handle(CreateSupplierCommand command, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(command.StoreId, cancellationToken))
            return new CreateSupplierResult(CreateSupplierOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
            return new CreateSupplierResult(CreateSupplierOutcome.Forbidden, null);

        if (!await storeAccessAuthorizer.IsOperationalAsync(command.StoreId, cancellationToken))
            return new CreateSupplierResult(CreateSupplierOutcome.SubscriptionInactive, null);

        var supplier = new Supplier
        {
            StoreId = command.StoreId,
            Name = command.Name,
            ContactPhone = command.ContactPhone,
            ContactEmail = command.ContactEmail
        };

        supplierRepository.Add(supplier);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateSupplierResult(CreateSupplierOutcome.Created, supplier.Id);
    }
}
