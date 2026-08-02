using Application.Abstractions;
using Application.Common;

namespace Application.Inventory.Commands.DeleteSupplier;

public sealed class DeleteSupplierCommandHandler(
    ISupplierRepository supplierRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteSupplierCommand, DeleteSupplierResult>
{
    public async Task<DeleteSupplierResult> Handle(DeleteSupplierCommand command, CancellationToken cancellationToken)
    {
        var supplier = await supplierRepository.GetByIdAsync(command.SupplierId, cancellationToken);
        if (supplier is null)
            return new DeleteSupplierResult(DeleteSupplierOutcome.NotFound);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(supplier.StoreId, command.PerformedByUserId, cancellationToken))
            return new DeleteSupplierResult(DeleteSupplierOutcome.Forbidden);

        if (await supplierRepository.IsInUseAsync(command.SupplierId, cancellationToken))
            return new DeleteSupplierResult(DeleteSupplierOutcome.InUse);

        supplierRepository.Remove(supplier);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteSupplierResult(DeleteSupplierOutcome.Deleted);
    }
}
