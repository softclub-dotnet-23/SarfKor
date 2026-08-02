using FluentValidation;

namespace Application.Customers.Queries.GetCustomerByPhone;

public sealed class GetCustomerByPhoneQueryValidator : AbstractValidator<GetCustomerByPhoneQuery>
{
    public GetCustomerByPhoneQueryValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30);
    }
}
