using FluentValidation;

namespace Application.Stores.Commands.ChangeStoreStatus;

public sealed class ChangeStoreStatusCommandValidator : AbstractValidator<ChangeStoreStatusCommand>
{
    public ChangeStoreStatusCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
        RuleFor(x => x.NewStatus).IsInEnum();
    }
}
