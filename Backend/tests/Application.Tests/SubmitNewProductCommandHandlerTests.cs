using Application.Abstractions;
using Application.Products.Commands.SubmitNewProduct;
using Domain.Auditing;
using Domain.Products;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

// ADMIN_PROMPT.md §1: no moderation queue at all anymore — every submission (partner or ordinary
// user) publishes a Product immediately. ProductSubmission is created alongside it purely as a
// provenance record, never as a separate pending state.
public class SubmitNewProductCommandHandlerTests
{
    private const string Barcode = "1234567890128";
    private const string UserId = "user-1";

    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IProductSubmissionRepository> _productSubmissionRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IBrandRepository> _brandRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private SubmitNewProductCommandHandler CreateHandler() => new(
        _productRepository.Object,
        _productSubmissionRepository.Object,
        _categoryRepository.Object,
        _brandRepository.Object,
        _auditLogRepository.Object,
        _unitOfWork.Object);

    private static SubmitNewProductCommand CreateCommand(bool createDirectly = false) =>
        new(Barcode, "Test product", 1, 1, "TJ", UserId, createDirectly);

    private void SetupHappyPath()
    {
        _productRepository.Setup(r => r.GetByBarcodeAsync(Barcode, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        _categoryRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _brandRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _productRepository.Setup(r => r.Add(It.IsAny<Product>())).Callback<Product>(p => p.Id = 7);
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
        _productRepository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
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
        _productRepository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
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
        _productRepository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrdinaryUser_CreatesProductAndProvenanceSubmission()
    {
        SetupHappyPath();
        _productSubmissionRepository.Setup(r => r.Add(It.IsAny<ProductSubmission>())).Callback<ProductSubmission>(s => s.Id = 42);

        var handler = CreateHandler();
        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(SubmitNewProductOutcome.Created, result.Outcome);
        Assert.Equal(42, result.ProductSubmissionId);
        Assert.Equal(7, result.ProductId);
        _productRepository.Verify(r => r.Add(It.Is<Product>(p => p.Barcode.Value == Barcode)), Times.Once);
        _productSubmissionRepository.Verify(
            r => r.Add(It.Is<ProductSubmission>(s => s.Barcode.Value == Barcode && s.SubmittedByUserId == UserId && s.ProductId == 7)),
            Times.Once);
        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(a => a.Action == "Product.CreatedByUser")), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_CreateDirectlyByStorePartner_CreatesProductWithPartnerAuditAction()
    {
        SetupHappyPath();

        var handler = CreateHandler();
        var result = await handler.Handle(CreateCommand(createDirectly: true), CancellationToken.None);

        Assert.Equal(SubmitNewProductOutcome.Created, result.Outcome);
        Assert.Equal(7, result.ProductId);
        _productRepository.Verify(r => r.Add(It.Is<Product>(p => p.Barcode.Value == Barcode)), Times.Once);
        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(a => a.Action == "Product.CreatedByPartner")), Times.Once);
    }
}
