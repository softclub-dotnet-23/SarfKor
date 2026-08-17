using FluentValidation;

namespace Application.Stores.Commands.SetStoreEmployeeActive;

public sealed class SetStoreEmployeeActiveCommandValidator : AbstractValidator<SetStoreEmployeeActiveCommand>
{
    public SetStoreEmployeeActiveCommandValidator()
    {
        RuleFor(x => x.StoreEmployeeId).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
