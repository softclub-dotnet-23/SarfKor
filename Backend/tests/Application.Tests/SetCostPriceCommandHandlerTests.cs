using Application.Abstractions;
using Application.Inventory.Commands.SetCostPrice;
using Domain.Inventory;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class SetCostPriceCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;
    private const int ProductId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICostPriceRepository> _costPriceRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private SetCostPriceCommandHandler CreateHandler() => new(
        _storeRepository.Object,
        _productRepository.Object,
        _costPriceRepository.Object,
        _unitOfWork.Object);

    private static SetCostPriceCommand ValidCommand() => new(StoreId, ProductId, Amount: 8, Currency: "TJS", PerformedByUserId: OwnerId);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(SetCostPriceOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand() with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(SetCostPriceOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ProductDoesNotExist_ReturnsProductNotFound()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });
        _productRepository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(SetCostPriceOutcome.ProductNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidCommand_RecordsCostPriceAndReturnsItsId()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });
        _productRepository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _costPriceRepository.Setup(r => r.Add(It.IsAny<CostPrice>())).Callback<CostPrice>(c => c.Id = 9);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(SetCostPriceOutcome.Set, result.Outcome);
        Assert.Equal(9, result.CostPriceId);
        _costPriceRepository.Verify(r => r.Add(It.Is<CostPrice>(c => c.Amount.Amount == 8 && c.ProductId == ProductId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
