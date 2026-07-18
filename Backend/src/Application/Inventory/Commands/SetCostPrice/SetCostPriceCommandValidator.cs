using FluentValidation;

namespace Application.Inventory.Commands.SetCostPrice;

public sealed class SetCostPriceCommandValidator : AbstractValidator<SetCostPriceCommand>
{
    public SetCostPriceCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
