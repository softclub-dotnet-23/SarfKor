using Application.Abstractions;
using Application.Catalog.Commands.UpdateBrand;
using Domain.Catalog;
using Moq;

namespace Application.Tests;

public class UpdateBrandCommandHandlerTests
{
    private readonly Mock<IBrandRepository> _brandRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateBrandCommandHandler CreateHandler() => new(_brandRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_BrandNotFound_ReturnsNotFound()
    {
        _brandRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Brand?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateBrandCommand(1, "New name"), CancellationToken.None);

        Assert.Equal(UpdateBrandOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_Valid_UpdatesName()
    {
        var brand = new Brand { Name = "Old name" };
        _brandRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(brand);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateBrandCommand(1, "New name"), CancellationToken.None);

        Assert.Equal(UpdateBrandOutcome.Updated, result.Outcome);
        Assert.Equal("New name", brand.Name);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
