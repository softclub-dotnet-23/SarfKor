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
    private readonly Mock<IReorderRuleRepository> _reorderRuleRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateReorderRuleCommandHandler CreateHandler() => new(_storeRepository.Object, _reorderRuleRepository.Object, _unitOfWork.Object);

    private static CreateReorderRuleCommand ValidCommand() => new(StoreId, ProductId: 1, ThresholdQuantity: 5, ReorderQuantity: 20, PreferredSupplierId: null, PerformedByUserId: OwnerId);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreateReorderRuleOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand() with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(CreateReorderRuleOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesActiveRule()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });
        _reorderRuleRepository.Setup(r => r.Add(It.IsAny<ReorderRule>())).Callback<ReorderRule>(rule => rule.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreateReorderRuleOutcome.Created, result.Outcome);
        Assert.Equal(1, result.ReorderRuleId);
        _reorderRuleRepository.Verify(r => r.Add(It.Is<ReorderRule>(rule => rule.IsActive)), Times.Once);
    }
}
