using Application.Common;
using FluentValidation;

namespace Application.Offers.Commands.PublishExpiringOffer;

public sealed class PublishExpiringOfferCommandValidator : AbstractValidator<PublishExpiringOfferCommand>
{
    public PublishExpiringOfferCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Must(SupportedCurrencies.IsSupported).WithMessage("Unsupported currency.");
        RuleFor(x => x.OriginalPrice).GreaterThan(0);
        RuleFor(x => x.DiscountedPrice).GreaterThan(0).LessThan(x => x.OriginalPrice);
        RuleFor(x => x.ExpiresAt).GreaterThan(DateTimeOffset.UtcNow);
    }
}
