using Application.Common;
using FluentValidation;

namespace Application.Sales.Commands.ProcessSale;

public sealed class ProcessSaleCommandValidator : AbstractValidator<ProcessSaleCommand>
{
    public ProcessSaleCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.CashierUserId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Must(SupportedCurrencies.IsSupported).WithMessage("Unsupported currency.");
        RuleFor(x => x).Must(x => x.Lines.Count > 0 || (x.BundleLines?.Count ?? 0) > 0)
            .WithMessage("At least one product line or bundle line is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
        RuleForEach(x => x.BundleLines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductBundleId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
        RuleFor(x => x.GiftCardCode).MaximumLength(50);
        RuleFor(x => x.CustomerId).GreaterThan(0).When(x => x.CustomerId.HasValue);
        RuleFor(x => x.CustomerId).NotNull().When(x => x.ApplyStoreCredit).WithMessage("CustomerId is required to apply store credit.");
    }
}
