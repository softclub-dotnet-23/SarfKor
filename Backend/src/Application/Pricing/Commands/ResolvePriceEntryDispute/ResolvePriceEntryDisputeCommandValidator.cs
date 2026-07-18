using FluentValidation;

namespace Application.Pricing.Commands.ResolvePriceEntryDispute;

public sealed class ResolvePriceEntryDisputeCommandValidator : AbstractValidator<ResolvePriceEntryDisputeCommand>
{
    public ResolvePriceEntryDisputeCommandValidator()
    {
        RuleFor(x => x.DisputeId).GreaterThan(0);
        RuleFor(x => x.AdminUserId).NotEmpty();
    }
}
