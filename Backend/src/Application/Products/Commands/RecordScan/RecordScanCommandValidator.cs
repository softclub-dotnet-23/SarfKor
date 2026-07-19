using FluentValidation;

namespace Application.Products.Commands.RecordScan;

public sealed class RecordScanCommandValidator : AbstractValidator<RecordScanCommand>
{
    public RecordScanCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.StoreId).GreaterThan(0).When(x => x.StoreId.HasValue);
    }
}
