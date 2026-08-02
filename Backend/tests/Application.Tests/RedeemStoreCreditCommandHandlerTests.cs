using Application.Abstractions;
using Application.Payments.Commands.RedeemStoreCredit;
using Domain.Payments;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class RedeemStoreCreditCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;
    private const int CustomerId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IStoreCreditRepository> _storeCreditRepository = new();

    private RedeemStoreCreditCommandHandler CreateHandler() =>
        new(_storeRepository.Object, _storeAccessAuthorizer.Object, _storeCreditRepository.Object);

    private static RedeemStoreCreditCommand ValidCommand() => new(StoreId, CustomerId, 10, "TJS", OwnerId);

    private void SetupOwnedStore()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerOrEmployeeAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(RedeemStoreCreditOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwnerOrEmployee_ReturnsForbidden()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand() with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(RedeemStoreCreditOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_NoCreditOnFile_ReturnsNoCreditOnFile()
    {
        SetupOwnedStore();
        _storeCreditRepository
            .Setup(r => r.GetByStoreAndCustomerAsync(StoreId, CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreCredit?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(RedeemStoreCreditOutcome.NoCreditOnFile, result.Outcome);
    }

    [Fact]
    public async Task Handle_CurrencyMismatch_ReturnsCurrencyMismatch()
    {
        SetupOwnedStore();
        _storeCreditRepository
            .Setup(r => r.GetByStoreAndCustomerAsync(StoreId, CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoreCredit { StoreId = StoreId, CustomerId = CustomerId, Balance = new Money(30, "USD"), UpdatedAt = DateTimeOffset.UtcNow });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(RedeemStoreCreditOutcome.CurrencyMismatch, result.Outcome);
    }

    [Fact]
    public async Task Handle_InsufficientBalance_ReturnsInsufficientBalance()
    {
        SetupOwnedStore();
        _storeCreditRepository
            .Setup(r => r.GetByStoreAndCustomerAsync(StoreId, CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoreCredit { StoreId = StoreId, CustomerId = CustomerId, Balance = new Money(5, "TJS"), UpdatedAt = DateTimeOffset.UtcNow });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(RedeemStoreCreditOutcome.InsufficientBalance, result.Outcome);
    }

    [Fact]
    public async Task Handle_ConcurrentRedemptionLosesRace_ReturnsInsufficientBalance()
    {
        SetupOwnedStore();
        var credit = new StoreCredit { Id = 1, StoreId = StoreId, CustomerId = CustomerId, Balance = new Money(30, "TJS"), UpdatedAt = DateTimeOffset.UtcNow };
        _storeCreditRepository
            .Setup(r => r.GetByStoreAndCustomerAsync(StoreId, CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credit);
        _storeCreditRepository.Setup(r => r.TryDebitAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(RedeemStoreCreditOutcome.InsufficientBalance, result.Outcome);
    }

    [Fact]
    public async Task Handle_SufficientBalance_DebitsAtomically()
    {
        SetupOwnedStore();
        var credit = new StoreCredit { Id = 1, StoreId = StoreId, CustomerId = CustomerId, Balance = new Money(30, "TJS"), UpdatedAt = DateTimeOffset.UtcNow };
        _storeCreditRepository
            .Setup(r => r.GetByStoreAndCustomerAsync(StoreId, CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credit);
        _storeCreditRepository.Setup(r => r.TryDebitAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(RedeemStoreCreditOutcome.Redeemed, result.Outcome);
        Assert.Equal(20, result.NewBalance);
        _storeCreditRepository.Verify(r => r.TryDebitAsync(1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
