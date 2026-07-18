namespace Application.Customers.Commands.CreateCustomer;

public sealed record CreateCustomerCommand(string PhoneNumber, string? FullName);
