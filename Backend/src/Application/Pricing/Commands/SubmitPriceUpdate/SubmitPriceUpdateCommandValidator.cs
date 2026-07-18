using FluentValidation;

namespace Application.Pricing.Commands.SubmitPriceUpdate;

public sealed class SubmitPriceUpdateCommandValidator : AbstractValidator<SubmitPriceUpdateCommand>
{
    public SubmitPriceUpdateCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
