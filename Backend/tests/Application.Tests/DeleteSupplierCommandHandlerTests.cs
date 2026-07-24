using Application.Abstractions;
using Application.Inventory.Commands.DeleteSupplier;
using Domain.Inventory;
using Moq;

namespace Application.Tests;

public class DeleteSupplierCommandHandlerTests
{
    private readonly Mock<ISupplierRepository> _supplierRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DeleteSupplierCommandHandler CreateHandler() => new(_supplierRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_SupplierNotFound_ReturnsNotFound()
    {
        _supplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteSupplierCommand(1), CancellationToken.None);

        Assert.Equal(DeleteSupplierOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_SupplierInUse_ReturnsInUseAndDoesNotDelete()
    {
        var supplier = new Supplier { Name = "Acme" };
        _supplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
        _supplierRepository.Setup(r => r.IsInUseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteSupplierCommand(1), CancellationToken.None);

        Assert.Equal(DeleteSupplierOutcome.InUse, result.Outcome);
        _supplierRepository.Verify(r => r.Remove(It.IsAny<Supplier>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotInUse_Deletes()
    {
        var supplier = new Supplier { Name = "Acme" };
        _supplierRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
        _supplierRepository.Setup(r => r.IsInUseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteSupplierCommand(1), CancellationToken.None);

        Assert.Equal(DeleteSupplierOutcome.Deleted, result.Outcome);
        _supplierRepository.Verify(r => r.Remove(supplier), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
