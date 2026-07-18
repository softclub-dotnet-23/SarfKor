using Application.Abstractions;
using Application.Pricing.Commands.SubmitPriceUpdate;
using Domain.Pricing;
using Domain.Reputation;
using Moq;

namespace Application.Tests;

public class SubmitPriceUpdateCommandHandlerTests
{
    private const int ProductId = 1;
    private const int StoreId = 1;
    private const string UserId = "user-1";

    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IPriceEntryRepository> _priceEntryRepository = new();
    private readonly Mock<IContributorTrustScoreRepository> _trustScoreRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private SubmitPriceUpdateCommandHandler CreateHandler() => new(
        _productRepository.Object,
        _storeRepository.Object,
        _priceEntryRepository.Object,
        _trustScoreRepository.Object,
        _unitOfWork.Object);

    private static SubmitPriceUpdateCommand ValidCommand() => new(ProductId, StoreId, UserId, Price: 12.5m, Currency: "TJS");

    [Fact]
    public async Task Handle_ProductDoesNotExist_ReturnsNull()
    {
        _productRepository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Null(result);
        _priceEntryRepository.Verify(r => r.Add(It.IsAny<PriceEntry>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StoreDoesNotExist_ReturnsNull()
    {
        _productRepository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Null(result);
        _priceEntryRepository.Verify(r => r.Add(It.IsAny<PriceEntry>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FirstContribution_CreatesPriceEntryAndDefaultTrustScore()
    {
        _productRepository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _trustScoreRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync((ContributorTrustScore?)null);
        _priceEntryRepository.Setup(r => r.Add(It.IsAny<PriceEntry>())).Callback<PriceEntry>(p => p.Id = 5);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(5, result!.PriceEntryId);
        _trustScoreRepository.Verify(r => r.Add(It.Is<ContributorTrustScore>(t => t.UserId == UserId && t.Score == 50)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturningContributor_DoesNotCreateDuplicateTrustScore()
    {
        _productRepository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _trustScoreRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContributorTrustScore { UserId = UserId, Score = 75, UpdatedAt = DateTimeOffset.UtcNow });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(result);
        _trustScoreRepository.Verify(r => r.Add(It.IsAny<ContributorTrustScore>()), Times.Never);
    }
}
