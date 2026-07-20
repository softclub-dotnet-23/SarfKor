using FluentValidation;

namespace Application.Stores.Queries.GetStoreEmployees;

public sealed class GetStoreEmployeesQueryValidator : AbstractValidator<GetStoreEmployeesQuery>
{
    public GetStoreEmployeesQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}
