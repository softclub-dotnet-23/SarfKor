using FluentValidation;

namespace Application.Inventory.Commands.RecordStockReceipt;

public sealed class RecordStockReceiptCommandValidator : AbstractValidator<RecordStockReceiptCommand>
{
    public RecordStockReceiptCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
