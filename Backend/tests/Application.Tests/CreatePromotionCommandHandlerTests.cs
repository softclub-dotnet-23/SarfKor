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
    private readonly Mock<IPromotionRepository> _promotionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreatePromotionCommandHandler CreateHandler() => new(_storeRepository.Object, _promotionRepository.Object, _unitOfWork.Object);

    private static CreatePromotionCommand ValidCommand() => new(
        StoreId, ProductId: 1, CategoryId: null,
        DiscountType: PromotionDiscountType.PercentageOff, DiscountValue: 20,
        StartsAt: DateTimeOffset.UtcNow, EndsAt: DateTimeOffset.UtcNow.AddDays(7),
        PerformedByUserId: OwnerId);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreatePromotionOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand() with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(CreatePromotionOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesPromotion()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });
        _promotionRepository.Setup(r => r.Add(It.IsAny<Promotion>())).Callback<Promotion>(p => p.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreatePromotionOutcome.Created, result.Outcome);
        Assert.Equal(1, result.PromotionId);
    }
}
