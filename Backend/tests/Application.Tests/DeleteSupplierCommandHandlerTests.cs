using Application.Abstractions;
using Application.Inventory.Commands.DeleteSupplier;
using Domain.Inventory;
using Moq;

namespace Application.Tests;

public class DeleteSupplierCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<ISupplierRepository> _supplierRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DeleteSupplierCommandHandler CreateHandler() => new(_supplierRepository.Object, _storeAccessAuthorizer.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_SupplierNotFound_ReturnsNotFound()
    {
        _supplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteSupplierCommand(1, OwnerId), CancellationToken.None);

        Assert.Equal(DeleteSupplierOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwnerOrEmployee_ReturnsForbidden()
    {
        var supplier = new Supplier { StoreId = StoreId, Name = "Acme" };
        _supplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerOrEmployeeAsync(StoreId, "someone-else", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteSupplierCommand(1, "someone-else"), CancellationToken.None);

        Assert.Equal(DeleteSupplierOutcome.Forbidden, result.Outcome);
        _supplierRepository.Verify(r => r.Remove(It.IsAny<Supplier>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SupplierInUse_ReturnsInUseAndDoesNotDelete()
    {
        var supplier = new Supplier { StoreId = StoreId, Name = "Acme" };
        _supplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerOrEmployeeAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _supplierRepository.Setup(r => r.IsInUseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteSupplierCommand(1, OwnerId), CancellationToken.None);

        Assert.Equal(DeleteSupplierOutcome.InUse, result.Outcome);
        _supplierRepository.Verify(r => r.Remove(It.IsAny<Supplier>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotInUse_Deletes()
    {
        var supplier = new Supplier { StoreId = StoreId, Name = "Acme" };
        _supplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerOrEmployeeAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _supplierRepository.Setup(r => r.IsInUseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteSupplierCommand(1, OwnerId), CancellationToken.None);

        Assert.Equal(DeleteSupplierOutcome.Deleted, result.Outcome);
        _supplierRepository.Verify(r => r.Remove(supplier), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
