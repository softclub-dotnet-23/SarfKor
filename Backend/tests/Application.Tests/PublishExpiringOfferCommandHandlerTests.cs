using Application.Abstractions;
using Application.Offers.Commands.PublishExpiringOffer;
using Domain.Offers;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class PublishExpiringOfferCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;
    private const int ProductId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IExpiringOfferRepository> _expiringOfferRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private PublishExpiringOfferCommandHandler CreateHandler() => new(
        _storeRepository.Object,
        _storeAccessAuthorizer.Object,
        _productRepository.Object,
        _expiringOfferRepository.Object,
        _unitOfWork.Object);

    private static PublishExpiringOfferCommand ValidCommand() => new(
        StoreId, ProductId, OriginalPrice: 20, DiscountedPrice: 15, Currency: "TJS",
        ExpiresAt: DateTimeOffset.UtcNow.AddDays(1), PerformedByUserId: OwnerId);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(PublishExpiringOfferOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand() with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(PublishExpiringOfferOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ProductDoesNotExist_ReturnsProductNotFound()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _productRepository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(PublishExpiringOfferOutcome.ProductNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesOfferAndReturnsItsId()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _productRepository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _expiringOfferRepository.Setup(r => r.Add(It.IsAny<ExpiringOffer>())).Callback<ExpiringOffer>(o => o.Id = 7);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(PublishExpiringOfferOutcome.Published, result.Outcome);
        Assert.Equal(7, result.OfferId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
