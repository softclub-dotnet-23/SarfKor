using FluentValidation;

namespace Application.Sales.Commands.VoidSale;

public sealed class VoidSaleCommandValidator : AbstractValidator<VoidSaleCommand>
{
    public VoidSaleCommandValidator()
    {
        RuleFor(x => x.SaleTransactionId).GreaterThan(0);
        RuleFor(x => x.VoidedByUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty();
    }
}
