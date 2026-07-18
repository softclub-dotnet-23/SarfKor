using Application.Abstractions;
using Domain.Customers;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CustomerRepository(AppDbContext dbContext) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken) =>
        dbContext.Customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

    public Task<Customer?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken) =>
        dbContext.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber, cancellationToken);

    public void Add(Customer customer) => dbContext.Customers.Add(customer);
}
