using Application.Abstractions;
using Application.Catalog.Commands.CreateProductBundle;
using Domain.Catalog;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class CreateProductBundleCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IProductBundleRepository> _productBundleRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateProductBundleCommandHandler CreateHandler() => new(_storeRepository.Object, _productBundleRepository.Object, _unitOfWork.Object);

    private static CreateProductBundleCommand ValidCommand() => new(
        StoreId, "Combo Deal", 25, "TJS", [new CreateProductBundleItemInput(1, 2), new CreateProductBundleItemInput(2, 1)], OwnerId);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreateProductBundleOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand() with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(CreateProductBundleOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesBundleWithItems()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });
        ProductBundle? added = null;
        _productBundleRepository.Setup(r => r.Add(It.IsAny<ProductBundle>())).Callback<ProductBundle>(b =>
        {
            b.Id = 1;
            added = b;
        });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreateProductBundleOutcome.Created, result.Outcome);
        Assert.Equal(1, result.ProductBundleId);
        Assert.Equal(2, added!.Items.Count);
        Assert.Equal(25, added.BundlePrice.Amount);
    }
}
