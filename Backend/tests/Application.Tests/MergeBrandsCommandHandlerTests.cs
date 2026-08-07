using Application.Abstractions;
using Application.Catalog.Commands.MergeBrands;
using Domain.Auditing;
using Domain.Catalog;
using Moq;

namespace Application.Tests;

public class MergeBrandsCommandHandlerTests
{
    private const string AdminUserId = "admin-1";

    private readonly Mock<IBrandRepository> _brandRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public MergeBrandsCommandHandlerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
    }

    private MergeBrandsCommandHandler CreateHandler() =>
        new(_brandRepository.Object, _productRepository.Object, _auditLogRepository.Object, _unitOfWork.Object);

    private static Brand CreateBrand(int id, string name)
    {
        var brand = new Brand { Name = name };
        brand.Id = id;
        return brand;
    }

    [Fact]
    public async Task Handle_TargetInSourceList_ReturnsTargetInSourceList()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(new MergeBrandsCommand(1, [1, 2], AdminUserId), CancellationToken.None);

        Assert.Equal(MergeBrandsOutcome.TargetInSourceList, result.Outcome);
    }

    [Fact]
    public async Task Handle_TargetNotFound_ReturnsTargetNotFound()
    {
        _brandRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Brand?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new MergeBrandsCommand(1, [2], AdminUserId), CancellationToken.None);

        Assert.Equal(MergeBrandsOutcome.TargetNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_SourceNotFound_ReturnsSourceNotFound()
    {
        _brandRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateBrand(1, "Target"));
        _brandRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((Brand?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new MergeBrandsCommand(1, [2], AdminUserId), CancellationToken.None);

        Assert.Equal(MergeBrandsOutcome.SourceNotFound, result.Outcome);
    }

    // ADMIN_PROMPT.md §2.8: "слияние обязано быть транзакционным ... не потерять ни одного товара" —
    // every source's products move to the target and every source brand is removed, none survive.
    [Fact]
    public async Task Handle_MultipleSources_MovesEveryProductAndRemovesEverySourceBrand()
    {
        var target = CreateBrand(1, "Coca-Cola");
        var source1 = CreateBrand(2, "Coca Cola");
        var source2 = CreateBrand(3, "coca cola");

        _brandRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        _brandRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(source1);
        _brandRepository.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(source2);

        _productRepository.Setup(r => r.ReassignBrandAsync(2, 1, It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _productRepository.Setup(r => r.ReassignBrandAsync(3, 1, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var handler = CreateHandler();
        var result = await handler.Handle(new MergeBrandsCommand(1, [2, 3], AdminUserId), CancellationToken.None);

        Assert.Equal(MergeBrandsOutcome.Merged, result.Outcome);
        Assert.Equal(8, result.ProductsMoved);
        _productRepository.Verify(r => r.ReassignBrandAsync(2, 1, It.IsAny<CancellationToken>()), Times.Once);
        _productRepository.Verify(r => r.ReassignBrandAsync(3, 1, It.IsAny<CancellationToken>()), Times.Once);
        _brandRepository.Verify(r => r.Remove(source1), Times.Once);
        _brandRepository.Verify(r => r.Remove(source2), Times.Once);
        _brandRepository.Verify(r => r.Remove(target), Times.Never);
        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(a => a.Action == "Brand.Merged" && a.EntityId == 1)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateSourceIds_ReassignsEachDistinctSourceOnlyOnce()
    {
        var target = CreateBrand(1, "Target");
        var source = CreateBrand(2, "Source");
        _brandRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        _brandRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _productRepository.Setup(r => r.ReassignBrandAsync(2, 1, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();
        var result = await handler.Handle(new MergeBrandsCommand(1, [2, 2], AdminUserId), CancellationToken.None);

        Assert.Equal(MergeBrandsOutcome.Merged, result.Outcome);
        Assert.Equal(1, result.ProductsMoved);
        _productRepository.Verify(r => r.ReassignBrandAsync(2, 1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
