using FluentValidation;

namespace Application.Receipts.Commands.VerifyReceipt;

public sealed class VerifyReceiptCommandValidator : AbstractValidator<VerifyReceiptCommand>
{
    public VerifyReceiptCommandValidator()
    {
        RuleFor(x => x.ReceiptId).GreaterThan(0);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}
