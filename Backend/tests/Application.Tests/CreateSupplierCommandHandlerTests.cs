using Application.Abstractions;
using Application.Inventory.Commands.CreateSupplier;
using Domain.Inventory;
using Moq;

namespace Application.Tests;

public class CreateSupplierCommandHandlerTests
{
    private readonly Mock<ISupplierRepository> _supplierRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Handle_CreatesSupplier_AndReturnsItsId()
    {
        _supplierRepository.Setup(r => r.Add(It.IsAny<Supplier>())).Callback<Supplier>(s => s.Id = 3);

        var handler = new CreateSupplierCommandHandler(_supplierRepository.Object, _unitOfWork.Object);
        var result = await handler.Handle(new CreateSupplierCommand("Global Foods LLC", "+992900000000", "contact@globalfoods.tj"), CancellationToken.None);

        Assert.Equal(3, result.SupplierId);
        _supplierRepository.Verify(r => r.Add(It.Is<Supplier>(s => s.Name == "Global Foods LLC" && s.ContactEmail == "contact@globalfoods.tj")), Times.Once);
    }
}
