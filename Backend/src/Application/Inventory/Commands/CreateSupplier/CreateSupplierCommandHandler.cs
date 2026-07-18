using Application.Abstractions;
using Application.Common;
using Domain.Inventory;

namespace Application.Inventory.Commands.CreateSupplier;

public sealed class CreateSupplierCommandHandler(
    ISupplierRepository supplierRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateSupplierCommand, CreateSupplierResult>
{
    public async Task<CreateSupplierResult> Handle(CreateSupplierCommand command, CancellationToken cancellationToken)
    {
        var supplier = new Supplier
        {
            Name = command.Name,
            ContactPhone = command.ContactPhone,
            ContactEmail = command.ContactEmail
        };

        supplierRepository.Add(supplier);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateSupplierResult(supplier.Id);
    }
}
