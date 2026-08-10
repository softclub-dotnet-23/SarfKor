using Application.Abstractions;
using Application.Inventory.Commands.CreateReorderRule;
using Domain.Inventory;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class CreateReorderRuleCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ISupplierRepository> _supplierRepository = new();
    private readonly Mock<IReorderRuleRepository> _reorderRuleRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateReorderRuleCommandHandler CreateHandler() => new(_storeRepository.Object, _storeAccessAuthorizer.Object, _productRepository.Object, _supplierRepository.Object, _reorderRuleRepository.Object, _unitOfWork.Object);

    private static CreateReorderRuleCommand ValidCommand() => new(StoreId, ProductId: 1, ThresholdQuantity: 5, ReorderQuantity: 20, PreferredSupplierId: null, PerformedByUserId: OwnerId);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreateReorderRuleOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand() with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(CreateReorderRuleOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesActiveRule()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _productRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _reorderRuleRepository.Setup(r => r.Add(It.IsAny<ReorderRule>())).Callback<ReorderRule>(rule => rule.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreateReorderRuleOutcome.Created, result.Outcome);
        Assert.Equal(1, result.ReorderRuleId);
        _reorderRuleRepository.Verify(r => r.Add(It.Is<ReorderRule>(rule => rule.IsActive)), Times.Once);
    }
}
