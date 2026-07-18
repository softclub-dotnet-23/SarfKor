using Application.Abstractions;
using Application.Common;

namespace Application.Customers.Queries.GetCustomerByPhone;

public sealed class GetCustomerByPhoneQueryHandler(ICustomerRepository customerRepository) : IQueryHandler<GetCustomerByPhoneQuery, GetCustomerByPhoneResult>
{
    public async Task<GetCustomerByPhoneResult> Handle(GetCustomerByPhoneQuery query, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByPhoneNumberAsync(query.PhoneNumber, cancellationToken);
        return customer is null
            ? new GetCustomerByPhoneResult(null, null)
            : new GetCustomerByPhoneResult(customer.Id, customer.FullName);
    }
}
