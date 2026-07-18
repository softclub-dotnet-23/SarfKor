using Application.Abstractions;
using Application.Common;
using Domain.Customers;

namespace Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateCustomerCommand, CreateCustomerResult>
{
    public async Task<CreateCustomerResult> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        // Customers are a shared registry keyed by phone number (a person can be a customer at
        // several stores) — re-registering the same number returns the existing record.
        var existing = await customerRepository.GetByPhoneNumberAsync(command.PhoneNumber, cancellationToken);
        if (existing is not null)
            return new CreateCustomerResult(existing.Id);

        var customer = new Customer { PhoneNumber = command.PhoneNumber, FullName = command.FullName, CreatedAt = DateTimeOffset.UtcNow };
        customerRepository.Add(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateCustomerResult(customer.Id);
    }
}
