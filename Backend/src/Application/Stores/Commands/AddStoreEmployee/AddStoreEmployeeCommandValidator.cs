using FluentValidation;

namespace Application.Stores.Commands.AddStoreEmployee;

public sealed class AddStoreEmployeeCommandValidator : AbstractValidator<AddStoreEmployeeCommand>
{
    public AddStoreEmployeeCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.EmployeeUserId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
