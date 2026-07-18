using FluentValidation;

namespace Application.Inventory.Commands.CompleteStockTransfer;

public sealed class CompleteStockTransferCommandValidator : AbstractValidator<CompleteStockTransferCommand>
{
    public CompleteStockTransferCommandValidator()
    {
        RuleFor(x => x.StockTransferId).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
