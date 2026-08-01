using Application.Common;
using FluentValidation;

namespace Application.Receipts.Commands.UploadReceipt;

public sealed class UploadReceiptCommandValidator : AbstractValidator<UploadReceiptCommand>
{
    public UploadReceiptCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ImageReference).NotEmpty();
        RuleFor(x => x.StoreId).GreaterThan(0).When(x => x.StoreId.HasValue);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).GreaterThan(0).When(l => l.ProductId.HasValue);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.Price).GreaterThan(0);
            line.RuleFor(l => l.Currency).NotEmpty().Must(SupportedCurrencies.IsSupported).WithMessage("Unsupported currency.");
        });
    }
}
