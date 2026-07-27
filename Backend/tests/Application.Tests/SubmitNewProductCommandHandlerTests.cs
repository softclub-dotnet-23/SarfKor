using Application.Abstractions;
using Application.Products.Commands.SubmitNewProduct;
using Domain.Products;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class SubmitNewProductCommandHandlerTests
{
    private const string Barcode = "1234567890128";
    private const string UserId = "user-1";

    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IProductSubmissionRepository> _productSubmissionRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IBrandRepository> _brandRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private SubmitNewProductCommandHandler CreateHandler() => new(
        _productRepository.Object,
        _productSubmissionRepository.Object,
        _categoryRepository.Object,
        _brandRepository.Object,
        _unitOfWork.Object);

    private static SubmitNewProductCommand CreateCommand() => new(Barcode, "Test product", 1, 1, "TJ", UserId);

    private void SetupHappyPath()
    {
        _productRepository.Setup(r => r.GetByBarcodeAsync(Barcode, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        _categoryRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _brandRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _productSubmissionRepository
            .Setup(r => r.GetPendingByBarcodeAsync(Barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductSubmission?)null);
    }

    [Fact]
    public async Task Handle_BarcodeAlreadyAProduct_ReturnsDuplicateBarcode()
    {
        _productRepository
            .Setup(r => r.GetByBarcodeAsync(Barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Barcode = new Barcode(Barcode), Name = "Existing", CountryOfOrigin = "TJ" });

        var handler = CreateHandler();
        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(SubmitNewProductOutcome.DuplicateBarcode, result.Outcome);
        _productSubmissionRepository.Verify(r => r.Add(It.IsAny<ProductSubmission>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsCategoryNotFound()
    {
        _productRepository.Setup(r => r.GetByBarcodeAsync(Barcode, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        _categoryRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(SubmitNewProductOutcome.CategoryNotFound, result.Outcome);
        _productSubmissionRepository.Verify(r => r.Add(It.IsAny<ProductSubmission>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BrandNotFound_ReturnsBrandNotFound()
    {
        _productRepository.Setup(r => r.GetByBarcodeAsync(Barcode, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        _categoryRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _brandRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(SubmitNewProductOutcome.BrandNotFound, result.Outcome);
        _productSubmissionRepository.Verify(r => r.Add(It.IsAny<ProductSubmission>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PendingSubmissionAlreadyExistsForBarcode_ReturnsDuplicatePendingSubmission()
    {
        _productRepository.Setup(r => r.GetByBarcodeAsync(Barcode, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        _categoryRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _brandRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _productSubmissionRepository
            .Setup(r => r.GetPendingByBarcodeAsync(Barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductSubmission
            {
                Barcode = new Barcode(Barcode),
                Name = "Someone else's submission",
                CountryOfOrigin = "TJ",
                SubmittedByUserId = "other-user",
                Status = ProductSubmissionStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            });

        var handler = CreateHandler();
        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(SubmitNewProductOutcome.DuplicatePendingSubmission, result.Outcome);
        _productSubmissionRepository.Verify(r => r.Add(It.IsAny<ProductSubmission>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Valid_CreatesPendingSubmissionAndReturnsItsId()
    {
        SetupHappyPath();
        _productSubmissionRepository.Setup(r => r.Add(It.IsAny<ProductSubmission>())).Callback<ProductSubmission>(s => s.Id = 42);

        var handler = CreateHandler();
        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(SubmitNewProductOutcome.Submitted, result.Outcome);
        Assert.Equal(42, result.ProductSubmissionId);
        _productSubmissionRepository.Verify(
            r => r.Add(It.Is<ProductSubmission>(s =>
                s.Barcode.Value == Barcode && s.Status == ProductSubmissionStatus.Pending && s.SubmittedByUserId == UserId)),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
