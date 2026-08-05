using FluentValidation;

namespace Application.Stores.Queries.GetStoreEmployeesForAdmin;

public sealed class GetStoreEmployeesForAdminQueryValidator : AbstractValidator<GetStoreEmployeesForAdminQuery>
{
    public GetStoreEmployeesForAdminQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
    }
}
