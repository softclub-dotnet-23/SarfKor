using Application.Abstractions;
using Application.Pricing.Commands.SubmitPriceUpdate;
using Domain.Pricing;
using Domain.Reputation;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class SubmitPriceUpdateCommandHandlerTests
{
    private const int ProductId = 1;
    private const int StoreId = 1;
    private const string UserId = "user-1";

    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreEmployeeRepository> _storeEmployeeRepository = new();
    private readonly Mock<IPriceEntryRepository> _priceEntryRepository = new();
    private readonly Mock<IContributorTrustScoreRepository> _trustScoreRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public SubmitPriceUpdateCommandHandlerTests() =>
        _storeEmployeeRepository
            .Setup(r => r.IsEmployeeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

    private SubmitPriceUpdateCommandHandler CreateHandler() => new(
        _productRepository.Object,
        _storeRepository.Object,
        _storeEmployeeRepository.Object,
        _priceEntryRepository.Object,
        _trustScoreRepository.Object,
        _unitOfWork.Object);

    private static SubmitPriceUpdateCommand ValidCommand() => new(ProductId, StoreId, UserId, Price: 12.5m, Currency: "TJS");

    private void SetupStoreOwnedBy(string ownerUserId) =>
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = ownerUserId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

    [Fact]
    public async Task Handle_ProductDoesNotExist_ReturnsProductNotFound()
    {
        _productRepository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(SubmitPriceUpdateOutcome.ProductNotFound, result.Outcome);
        _priceEntryRepository.Verify(r => r.Add(It.IsAny<PriceEntry>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StoreDoesNotExist_ReturnsStoreNotFound()
    {
        _productRepository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(SubmitPriceUpdateOutcome.StoreNotFound, result.Outcome);
        _priceEntryRepository.Verify(r => r.Add(It.IsAny<PriceEntry>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CallerNotStoreOwnerOrEmployee_ReturnsForbidden()
    {
        _productRepository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        SetupStoreOwnedBy("someone-else");

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(SubmitPriceUpdateOutcome.Forbidden, result.Outcome);
        _priceEntryRepository.Verify(r => r.Add(It.IsAny<PriceEntry>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FirstContribution_CreatesPriceEntryAndDefaultTrustScore()
    {
        _productRepository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        SetupStoreOwnedBy(UserId);
        _trustScoreRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync((ContributorTrustScore?)null);
        _priceEntryRepository.Setup(r => r.Add(It.IsAny<PriceEntry>())).Callback<PriceEntry>(p => p.Id = 5);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(SubmitPriceUpdateOutcome.Submitted, result.Outcome);
        Assert.Equal(5, result.PriceEntryId);
        _trustScoreRepository.Verify(r => r.Add(It.Is<ContributorTrustScore>(t => t.UserId == UserId && t.Score == 50)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturningContributor_DoesNotCreateDuplicateTrustScore()
    {
        _productRepository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        SetupStoreOwnedBy(UserId);
        _trustScoreRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContributorTrustScore { UserId = UserId, Score = 75, UpdatedAt = DateTimeOffset.UtcNow });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(SubmitPriceUpdateOutcome.Submitted, result.Outcome);
        _trustScoreRepository.Verify(r => r.Add(It.IsAny<ContributorTrustScore>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CallerIsStoreEmployeeNotOwner_Succeeds()
    {
        _productRepository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        SetupStoreOwnedBy("someone-else");
        _storeEmployeeRepository
            .Setup(r => r.IsEmployeeAsync(StoreId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _trustScoreRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync((ContributorTrustScore?)null);
        _priceEntryRepository.Setup(r => r.Add(It.IsAny<PriceEntry>())).Callback<PriceEntry>(p => p.Id = 7);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(SubmitPriceUpdateOutcome.Submitted, result.Outcome);
        Assert.Equal(7, result.PriceEntryId);
    }
}
