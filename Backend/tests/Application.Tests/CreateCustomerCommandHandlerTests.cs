using Application.Abstractions;
using Application.Customers.Commands.CreateCustomer;
using Domain.Customers;
using Moq;

namespace Application.Tests;

public class CreateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateCustomerCommandHandler CreateHandler() => new(_customerRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_NewPhoneNumber_CreatesCustomer()
    {
        _customerRepository.Setup(r => r.GetByPhoneNumberAsync("+992900000000", It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);
        _customerRepository.Setup(r => r.Add(It.IsAny<Customer>())).Callback<Customer>(c => c.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateCustomerCommand("+992900000000", "Ali"), CancellationToken.None);

        Assert.Equal(1, result.CustomerId);
        _customerRepository.Verify(r => r.Add(It.IsAny<Customer>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingPhoneNumber_ReturnsExistingCustomerWithoutDuplicating()
    {
        var existing = new Customer { PhoneNumber = "+992900000000", FullName = "Ali", CreatedAt = DateTimeOffset.UtcNow };
        existing.Id = 5;
        _customerRepository.Setup(r => r.GetByPhoneNumberAsync("+992900000000", It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateCustomerCommand("+992900000000", "Ali"), CancellationToken.None);

        Assert.Equal(5, result.CustomerId);
        _customerRepository.Verify(r => r.Add(It.IsAny<Customer>()), Times.Never);
    }
}
