using FluentValidation;

namespace Application.Payments.Commands.IssueStoreCredit;

public sealed class IssueStoreCreditCommandValidator : AbstractValidator<IssueStoreCreditCommand>
{
    public IssueStoreCreditCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
