using Application.Abstractions;
using Application.Inventory.Commands.UpdateSupplier;
using Domain.Inventory;
using Moq;

namespace Application.Tests;

public class UpdateSupplierCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<ISupplierRepository> _supplierRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateSupplierCommandHandler CreateHandler() => new(_supplierRepository.Object, _storeAccessAuthorizer.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_SupplierNotFound_ReturnsNotFound()
    {
        _supplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateSupplierCommand(1, OwnerId, "Acme", null, null), CancellationToken.None);

        Assert.Equal(UpdateSupplierOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwnerOrEmployee_ReturnsForbidden()
    {
        var supplier = new Supplier { StoreId = StoreId, Name = "Old Co" };
        _supplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerOrEmployeeAsync(StoreId, "someone-else", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateSupplierCommand(1, "someone-else", "New Co", null, null), CancellationToken.None);

        Assert.Equal(UpdateSupplierOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_Valid_UpdatesFields()
    {
        var supplier = new Supplier { StoreId = StoreId, Name = "Old Co", ContactPhone = null, ContactEmail = null };
        _supplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerOrEmployeeAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateSupplierCommand(1, OwnerId, "New Co", "+992123456", "new@co.tj"), CancellationToken.None);

        Assert.Equal(UpdateSupplierOutcome.Updated, result.Outcome);
        Assert.Equal("New Co", supplier.Name);
        Assert.Equal("+992123456", supplier.ContactPhone);
        Assert.Equal("new@co.tj", supplier.ContactEmail);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
