using Application.Abstractions;
using Application.Offers.Commands.CreatePromotion;
using Domain.Offers;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class CreatePromotionCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IPromotionRepository> _promotionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreatePromotionCommandHandler CreateHandler() => new(_storeRepository.Object, _storeAccessAuthorizer.Object, _productRepository.Object, _categoryRepository.Object, _promotionRepository.Object, _unitOfWork.Object);

    private static CreatePromotionCommand ValidCommand() => new(
        StoreId, ProductId: 1, CategoryId: null,
        DiscountType: PromotionDiscountType.PercentageOff, DiscountValue: 20,
        StartsAt: DateTimeOffset.UtcNow, EndsAt: DateTimeOffset.UtcNow.AddDays(7),
        PerformedByUserId: OwnerId);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreatePromotionOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand() with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(CreatePromotionOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesPromotion()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _productRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _promotionRepository.Setup(r => r.Add(It.IsAny<Promotion>())).Callback<Promotion>(p => p.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreatePromotionOutcome.Created, result.Outcome);
        Assert.Equal(1, result.PromotionId);
    }
}
