using Application.Abstractions;
using Application.Common;

namespace Application.Inventory.Commands.UpdateSupplier;

public sealed class UpdateSupplierCommandHandler(ISupplierRepository supplierRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateSupplierCommand, UpdateSupplierResult>
{
    public async Task<UpdateSupplierResult> Handle(UpdateSupplierCommand command, CancellationToken cancellationToken)
    {
        var supplier = await supplierRepository.GetByIdAsync(command.SupplierId, cancellationToken);
        if (supplier is null)
            return new UpdateSupplierResult(UpdateSupplierOutcome.NotFound);

        supplier.Name = command.Name;
        supplier.ContactPhone = command.ContactPhone;
        supplier.ContactEmail = command.ContactEmail;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateSupplierResult(UpdateSupplierOutcome.Updated);
    }
}
