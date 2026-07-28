using Application.Abstractions;
using Application.Catalog.Commands.DeleteBrand;
using Domain.Catalog;
using Moq;

namespace Application.Tests;

public class DeleteBrandCommandHandlerTests
{
    private readonly Mock<IBrandRepository> _brandRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DeleteBrandCommandHandler CreateHandler() => new(_brandRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_BrandNotFound_ReturnsNotFound()
    {
        _brandRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Brand?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteBrandCommand(1), CancellationToken.None);

        Assert.Equal(DeleteBrandOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_BrandInUse_ReturnsInUseAndDoesNotDelete()
    {
        var brand = new Brand { Name = "Coca-Cola" };
        _brandRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(brand);
        _brandRepository.Setup(r => r.IsInUseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteBrandCommand(1), CancellationToken.None);

        Assert.Equal(DeleteBrandOutcome.InUse, result.Outcome);
        _brandRepository.Verify(r => r.Remove(It.IsAny<Brand>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotInUse_Deletes()
    {
        var brand = new Brand { Name = "Coca-Cola" };
        _brandRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(brand);
        _brandRepository.Setup(r => r.IsInUseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteBrandCommand(1), CancellationToken.None);

        Assert.Equal(DeleteBrandOutcome.Deleted, result.Outcome);
        _brandRepository.Verify(r => r.Remove(brand), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
