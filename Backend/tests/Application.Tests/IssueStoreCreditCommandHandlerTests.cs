using Application.Abstractions;
using Application.Payments.Commands.IssueStoreCredit;
using Domain.Customers;
using Domain.Payments;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class IssueStoreCreditCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;
    private const int CustomerId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IStoreCreditRepository> _storeCreditRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private IssueStoreCreditCommandHandler CreateHandler() => new(
        _storeRepository.Object,
        _storeAccessAuthorizer.Object,
        _customerRepository.Object,
        _storeCreditRepository.Object,
        _unitOfWork.Object);

    private static IssueStoreCreditCommand ValidCommand() => new(StoreId, CustomerId, 20, "TJS", OwnerId);

    private void SetupOwnedStore()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(IssueStoreCreditOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        SetupOwnedStore();

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand() with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(IssueStoreCreditOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ReturnsCustomerNotFound()
    {
        SetupOwnedStore();
        _customerRepository.Setup(r => r.GetByIdAsync(CustomerId, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(IssueStoreCreditOutcome.CustomerNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NoExistingCredit_CreatesNewRow()
    {
        SetupOwnedStore();
        _customerRepository
            .Setup(r => r.GetByIdAsync(CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { PhoneNumber = "+992900000000", CreatedAt = DateTimeOffset.UtcNow });
        _storeCreditRepository
            .Setup(r => r.GetByStoreAndCustomerAsync(StoreId, CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreCredit?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(IssueStoreCreditOutcome.Issued, result.Outcome);
        Assert.Equal(20, result.NewBalance);
        _storeCreditRepository.Verify(r => r.Add(It.IsAny<StoreCredit>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingCredit_IncrementsBalanceRatherThanCreatingNewRow()
    {
        SetupOwnedStore();
        _customerRepository
            .Setup(r => r.GetByIdAsync(CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { PhoneNumber = "+992900000000", CreatedAt = DateTimeOffset.UtcNow });
        _storeCreditRepository
            .Setup(r => r.GetByStoreAndCustomerAsync(StoreId, CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoreCredit { StoreId = StoreId, CustomerId = CustomerId, Balance = new Money(15, "TJS"), UpdatedAt = DateTimeOffset.UtcNow });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(IssueStoreCreditOutcome.Issued, result.Outcome);
        Assert.Equal(35, result.NewBalance);
        _storeCreditRepository.Verify(r => r.Add(It.IsAny<StoreCredit>()), Times.Never);
    }
}
