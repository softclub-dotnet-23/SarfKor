using FluentValidation;

namespace Application.Inventory.Commands.CreateReorderRule;

public sealed class CreateReorderRuleCommandValidator : AbstractValidator<CreateReorderRuleCommand>
{
    public CreateReorderRuleCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.ThresholdQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderQuantity).GreaterThan(0);
        RuleFor(x => x.PreferredSupplierId).GreaterThan(0).When(x => x.PreferredSupplierId.HasValue);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
