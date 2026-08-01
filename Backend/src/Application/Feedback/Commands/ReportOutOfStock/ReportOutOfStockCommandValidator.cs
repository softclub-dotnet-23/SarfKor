using FluentValidation;

namespace Application.Feedback.Commands.ReportOutOfStock;

public sealed class ReportOutOfStockCommandValidator : AbstractValidator<ReportOutOfStockCommand>
{
    public ReportOutOfStockCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.StoreId).GreaterThan(0).When(x => x.StoreId.HasValue);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
    }
}
