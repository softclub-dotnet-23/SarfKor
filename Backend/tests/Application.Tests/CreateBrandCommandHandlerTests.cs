using Application.Abstractions;
using Application.Catalog.Commands.CreateBrand;
using Domain.Catalog;
using Moq;

namespace Application.Tests;

public class CreateBrandCommandHandlerTests
{
    private readonly Mock<IBrandRepository> _brandRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Handle_CreatesBrand_AndReturnsItsId()
    {
        _brandRepository.Setup(r => r.Add(It.IsAny<Brand>())).Callback<Brand>(b => b.Id = 4);

        var handler = new CreateBrandCommandHandler(_brandRepository.Object, _unitOfWork.Object);
        var result = await handler.Handle(new CreateBrandCommand("Coca-Cola"), CancellationToken.None);

        Assert.Equal(4, result.BrandId);
        _brandRepository.Verify(r => r.Add(It.Is<Brand>(b => b.Name == "Coca-Cola")), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
