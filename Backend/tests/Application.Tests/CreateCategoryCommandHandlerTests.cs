using Application.Abstractions;
using Application.Catalog.Commands.CreateCategory;
using Domain.Catalog;
using Moq;

namespace Application.Tests;

public class CreateCategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateCategoryCommandHandler CreateHandler() => new(_categoryRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_NoParent_CreatesTopLevelCategory()
    {
        _categoryRepository.Setup(r => r.Add(It.IsAny<Category>())).Callback<Category>(c => c.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateCategoryCommand("Dairy", null), CancellationToken.None);

        Assert.Equal(CreateCategoryOutcome.Created, result.Outcome);
        Assert.Equal(1, result.CategoryId);
    }

    [Fact]
    public async Task Handle_ParentCategoryDoesNotExist_ReturnsParentCategoryNotFound()
    {
        _categoryRepository.Setup(r => r.ExistsAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateCategoryCommand("Yogurt", 99), CancellationToken.None);

        Assert.Equal(CreateCategoryOutcome.ParentCategoryNotFound, result.Outcome);
        _categoryRepository.Verify(r => r.Add(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidParent_CreatesSubCategory()
    {
        _categoryRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _categoryRepository.Setup(r => r.Add(It.IsAny<Category>())).Callback<Category>(c => c.Id = 2);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateCategoryCommand("Yogurt", 1), CancellationToken.None);

        Assert.Equal(CreateCategoryOutcome.Created, result.Outcome);
        Assert.Equal(2, result.CategoryId);
    }
}
