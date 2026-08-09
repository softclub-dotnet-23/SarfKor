using FluentValidation;

namespace Application.Stores.Commands.ResetCashierPassword;

public sealed class ResetCashierPasswordCommandValidator : AbstractValidator<ResetCashierPasswordCommand>
{
    public ResetCashierPasswordCommandValidator()
    {
        RuleFor(x => x.StoreEmployeeId).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
