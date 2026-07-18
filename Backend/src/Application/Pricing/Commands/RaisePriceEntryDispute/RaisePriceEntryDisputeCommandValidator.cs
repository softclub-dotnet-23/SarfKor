using FluentValidation;

namespace Application.Pricing.Commands.RaisePriceEntryDispute;

public sealed class RaisePriceEntryDisputeCommandValidator : AbstractValidator<RaisePriceEntryDisputeCommand>
{
    public RaisePriceEntryDisputeCommandValidator()
    {
        RuleFor(x => x.PriceEntryId).GreaterThan(0);
        RuleFor(x => x.DisputedByUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
