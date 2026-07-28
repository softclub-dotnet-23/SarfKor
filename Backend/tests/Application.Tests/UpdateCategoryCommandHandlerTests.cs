using Application.Abstractions;
using Application.Catalog.Commands.UpdateCategory;
using Domain.Catalog;
using Moq;

namespace Application.Tests;

public class UpdateCategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateCategoryCommandHandler CreateHandler() => new(_categoryRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsNotFound()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateCategoryCommand(1, "Snacks", null), CancellationToken.None);

        Assert.Equal(UpdateCategoryOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_ParentIsSelf_ReturnsSelfReference()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Category { Name = "Snacks" });

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateCategoryCommand(1, "Snacks", 1), CancellationToken.None);

        Assert.Equal(UpdateCategoryOutcome.SelfReference, result.Outcome);
    }

    [Fact]
    public async Task Handle_ParentCategoryNotFound_ReturnsParentCategoryNotFound()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Category { Name = "Snacks" });
        _categoryRepository.Setup(r => r.ExistsAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateCategoryCommand(1, "Snacks", 99), CancellationToken.None);

        Assert.Equal(UpdateCategoryOutcome.ParentCategoryNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_Valid_UpdatesNameAndParent()
    {
        var category = new Category { Name = "Old name", ParentCategoryId = null };
        _categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categoryRepository.Setup(r => r.ExistsAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateCategoryCommand(1, "New name", 2), CancellationToken.None);

        Assert.Equal(UpdateCategoryOutcome.Updated, result.Outcome);
        Assert.Equal("New name", category.Name);
        Assert.Equal(2, category.ParentCategoryId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
