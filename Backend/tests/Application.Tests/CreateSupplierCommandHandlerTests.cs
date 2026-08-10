using Application.Abstractions;
using Application.Inventory.Commands.CreateSupplier;
using Domain.Inventory;
using Moq;

namespace Application.Tests;

public class CreateSupplierCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<ISupplierRepository> _supplierRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateSupplierCommandHandler CreateHandler() => new(_storeRepository.Object, _storeAccessAuthorizer.Object, _supplierRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateSupplierCommand(StoreId, OwnerId, "Global Foods LLC", "+992900000000", "contact@globalfoods.tj"), CancellationToken.None);

        Assert.Equal(CreateSupplierOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwnerOrEmployee_ReturnsForbidden()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerOrEmployeeAsync(StoreId, "someone-else", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateSupplierCommand(StoreId, "someone-else", "Global Foods LLC", "+992900000000", "contact@globalfoods.tj"), CancellationToken.None);

        Assert.Equal(CreateSupplierOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_CreatesSupplier_AndReturnsItsId()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerOrEmployeeAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _supplierRepository.Setup(r => r.Add(It.IsAny<Supplier>())).Callback<Supplier>(s => s.Id = 3);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateSupplierCommand(StoreId, OwnerId, "Global Foods LLC", "+992900000000", "contact@globalfoods.tj"), CancellationToken.None);

        Assert.Equal(CreateSupplierOutcome.Created, result.Outcome);
        Assert.Equal(3, result.SupplierId);
        _supplierRepository.Verify(r => r.Add(It.Is<Supplier>(s => s.StoreId == StoreId && s.Name == "Global Foods LLC" && s.ContactEmail == "contact@globalfoods.tj")), Times.Once);
    }
}
