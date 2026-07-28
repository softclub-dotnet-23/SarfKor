using Application.Abstractions;
using Application.Inventory.Commands.UpdateSupplier;
using Domain.Inventory;
using Moq;

namespace Application.Tests;

public class UpdateSupplierCommandHandlerTests
{
    private readonly Mock<ISupplierRepository> _supplierRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateSupplierCommandHandler CreateHandler() => new(_supplierRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_SupplierNotFound_ReturnsNotFound()
    {
        _supplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateSupplierCommand(1, "Acme", null, null), CancellationToken.None);

        Assert.Equal(UpdateSupplierOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_Valid_UpdatesFields()
    {
        var supplier = new Supplier { Name = "Old Co", ContactPhone = null, ContactEmail = null };
        _supplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateSupplierCommand(1, "New Co", "+992123456", "new@co.tj"), CancellationToken.None);

        Assert.Equal(UpdateSupplierOutcome.Updated, result.Outcome);
        Assert.Equal("New Co", supplier.Name);
        Assert.Equal("+992123456", supplier.ContactPhone);
        Assert.Equal("new@co.tj", supplier.ContactEmail);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
