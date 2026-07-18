using FluentValidation;

namespace Application.Sales.Commands.ProcessReturn;

public sealed class ProcessReturnCommandValidator : AbstractValidator<ProcessReturnCommand>
{
    public ProcessReturnCommandValidator()
    {
        RuleFor(x => x.SaleTransactionId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.SaleLineItemId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}
