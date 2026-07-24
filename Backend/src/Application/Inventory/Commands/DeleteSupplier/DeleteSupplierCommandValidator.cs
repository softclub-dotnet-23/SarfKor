using FluentValidation;

namespace Application.Inventory.Commands.DeleteSupplier;

public sealed class DeleteSupplierCommandValidator : AbstractValidator<DeleteSupplierCommand>
{
    public DeleteSupplierCommandValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
    }
}
