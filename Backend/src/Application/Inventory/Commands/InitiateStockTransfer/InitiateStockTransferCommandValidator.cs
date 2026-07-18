using FluentValidation;

namespace Application.Inventory.Commands.InitiateStockTransfer;

public sealed class InitiateStockTransferCommandValidator : AbstractValidator<InitiateStockTransferCommand>
{
    public InitiateStockTransferCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.FromStoreId).GreaterThan(0);
        RuleFor(x => x.ToStoreId).GreaterThan(0).NotEqual(x => x.FromStoreId).WithMessage("Source and destination store must differ.");
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
