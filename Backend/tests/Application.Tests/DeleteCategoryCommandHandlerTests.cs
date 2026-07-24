using Application.Abstractions;
using Application.Catalog.Commands.DeleteCategory;
using Domain.Catalog;
using Moq;

namespace Application.Tests;

public class DeleteCategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DeleteCategoryCommandHandler CreateHandler() => new(_categoryRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsNotFound()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteCategoryCommand(1), CancellationToken.None);

        Assert.Equal(DeleteCategoryOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_CategoryInUse_ReturnsInUseAndDoesNotDelete()
    {
        var category = new Category { Name = "Snacks" };
        _categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categoryRepository.Setup(r => r.IsInUseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteCategoryCommand(1), CancellationToken.None);

        Assert.Equal(DeleteCategoryOutcome.InUse, result.Outcome);
        _categoryRepository.Verify(r => r.Remove(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotInUse_Deletes()
    {
        var category = new Category { Name = "Snacks" };
        _categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categoryRepository.Setup(r => r.IsInUseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteCategoryCommand(1), CancellationToken.None);

        Assert.Equal(DeleteCategoryOutcome.Deleted, result.Outcome);
        _categoryRepository.Verify(r => r.Remove(category), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
