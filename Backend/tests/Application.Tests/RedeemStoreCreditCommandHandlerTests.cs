using Application.Abstractions;
using Application.Payments.Commands.RedeemStoreCredit;
using Domain.Payments;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class RedeemStoreCreditCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;
    private const int CustomerId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreCreditRepository> _storeCreditRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RedeemStoreCreditCommandHandler CreateHandler() => new(_storeRepository.Object, _storeCreditRepository.Object, _unitOfWork.Object);

    private static RedeemStoreCreditCommand ValidCommand() => new(StoreId, CustomerId, 10, OwnerId);

    private void SetupOwnedStore() =>
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(RedeemStoreCreditOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        SetupOwnedStore();

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
    public async Task Handle_SufficientBalance_DecrementsBalance()
    {
        SetupOwnedStore();
        var credit = new StoreCredit { StoreId = StoreId, CustomerId = CustomerId, Balance = new Money(30, "TJS"), UpdatedAt = DateTimeOffset.UtcNow };
        _storeCreditRepository
            .Setup(r => r.GetByStoreAndCustomerAsync(StoreId, CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credit);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(RedeemStoreCreditOutcome.Redeemed, result.Outcome);
        Assert.Equal(20, result.NewBalance);
        Assert.Equal(20, credit.Balance.Amount);
    }
}
