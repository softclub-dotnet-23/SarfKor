using Application.Abstractions;
using Application.Common;

namespace Application.Inventory.Commands.UpdateSupplier;

public sealed class UpdateSupplierCommandHandler(
    ISupplierRepository supplierRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateSupplierCommand, UpdateSupplierResult>
{
    public async Task<UpdateSupplierResult> Handle(UpdateSupplierCommand command, CancellationToken cancellationToken)
    {
        var supplier = await supplierRepository.GetByIdAsync(command.SupplierId, cancellationToken);
        if (supplier is null)
            return new UpdateSupplierResult(UpdateSupplierOutcome.NotFound);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(supplier.StoreId, command.PerformedByUserId, cancellationToken))
            return new UpdateSupplierResult(UpdateSupplierOutcome.Forbidden);

        if (!await storeAccessAuthorizer.IsOperationalAsync(supplier.StoreId, cancellationToken))
            return new UpdateSupplierResult(UpdateSupplierOutcome.SubscriptionInactive);

        supplier.Name = command.Name;
        supplier.ContactPhone = command.ContactPhone;
        supplier.ContactEmail = command.ContactEmail;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateSupplierResult(UpdateSupplierOutcome.Updated);
    }
}
