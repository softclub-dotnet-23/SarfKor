using FluentValidation;

namespace Application.Stores.Commands.RemoveStoreEmployee;

public sealed class RemoveStoreEmployeeCommandValidator : AbstractValidator<RemoveStoreEmployeeCommand>
{
    public RemoveStoreEmployeeCommandValidator()
    {
        RuleFor(x => x.StoreEmployeeId).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
