using Domain.Offers;
using FluentValidation;

namespace Application.Offers.Commands.CreatePromotion;

public sealed class CreatePromotionCommandValidator : AbstractValidator<CreatePromotionCommand>
{
    public CreatePromotionCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0).When(x => x.ProductId.HasValue);
        RuleFor(x => x.CategoryId).GreaterThan(0).When(x => x.CategoryId.HasValue);
        RuleFor(x => x.DiscountType).IsInEnum();
        RuleFor(x => x.PerformedByUserId).NotEmpty();
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt);

        RuleFor(x => x.DiscountValue)
            .InclusiveBetween(0, 100)
            .When(x => x.DiscountType == PromotionDiscountType.PercentageOff)
            .WithMessage("Percentage discount must be between 0 and 100.");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0)
            .When(x => x.DiscountType == PromotionDiscountType.FixedAmountOff)
            .WithMessage("Fixed discount amount must be greater than 0.");
    }
}
