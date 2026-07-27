using FluentValidation;

namespace Application.Stores.Commands.AddStoreEmployee;

public sealed class AddStoreEmployeeCommandValidator : AbstractValidator<AddStoreEmployeeCommand>
{
    public AddStoreEmployeeCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.EmployeeEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
