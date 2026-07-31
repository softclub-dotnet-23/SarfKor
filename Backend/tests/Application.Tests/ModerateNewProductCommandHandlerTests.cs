using Application.Abstractions;
using Application.Products.Commands.ModerateNewProduct;
using Domain.Auditing;
using Domain.Products;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class ModerateNewProductCommandHandlerTests
{
    private const int SubmissionId = 1;
    private const string AdminUserId = "admin-1";
    private const string SubmitterUserId = "user-1";

    private readonly Mock<IProductSubmissionRepository> _productSubmissionRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public ModerateNewProductCommandHandlerTests() =>
        _productRepository.Setup(r => r.GetByBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

    private ModerateNewProductCommandHandler CreateHandler() => new(
        _productSubmissionRepository.Object,
        _productRepository.Object,
        _auditLogRepository.Object,
        _unitOfWork.Object);

    private static ProductSubmission CreateSubmission(ProductSubmissionStatus status) => new()
    {
        Barcode = new Barcode("1234567890128"),
        Name = "Test product",
        CountryOfOrigin = "TJ",
        SubmittedByUserId = SubmitterUserId,
        Status = status,
        CreatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Handle_SubmissionNotFound_ReturnsNotFound()
    {
        _productSubmissionRepository.Setup(r => r.GetByIdAsync(SubmissionId, It.IsAny<CancellationToken>())).ReturnsAsync((ProductSubmission?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new ModerateNewProductCommand(SubmissionId, true, AdminUserId, null, RequireOwnSubmission: false), CancellationToken.None);

        Assert.Equal(ModerateNewProductOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_AlreadyModerated_ReturnsAlreadyModerated()
    {
        _productSubmissionRepository
            .Setup(r => r.GetByIdAsync(SubmissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSubmission(ProductSubmissionStatus.Approved));

        var handler = CreateHandler();
        var result = await handler.Handle(new ModerateNewProductCommand(SubmissionId, true, AdminUserId, null, RequireOwnSubmission: false), CancellationToken.None);

        Assert.Equal(ModerateNewProductOutcome.AlreadyModerated, result.Outcome);
        _productRepository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Approve_CreatesProductAndReturnsItsId()
    {
        var submission = CreateSubmission(ProductSubmissionStatus.Pending);
        _productSubmissionRepository.Setup(r => r.GetByIdAsync(SubmissionId, It.IsAny<CancellationToken>())).ReturnsAsync(submission);
        _productRepository.Setup(r => r.Add(It.IsAny<Product>())).Callback<Product>(p => p.Id = 99);

        var handler = CreateHandler();
        var result = await handler.Handle(new ModerateNewProductCommand(SubmissionId, true, AdminUserId, null, RequireOwnSubmission: false), CancellationToken.None);

        Assert.Equal(ModerateNewProductOutcome.Approved, result.Outcome);
        Assert.Equal(99, result.ProductId);
        Assert.Equal(ProductSubmissionStatus.Approved, submission.Status);
        _productRepository.Verify(r => r.Add(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Reject_DoesNotCreateProduct()
    {
        var submission = CreateSubmission(ProductSubmissionStatus.Pending);
        _productSubmissionRepository.Setup(r => r.GetByIdAsync(SubmissionId, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var handler = CreateHandler();
        var result = await handler.Handle(new ModerateNewProductCommand(SubmissionId, false, AdminUserId, "duplicate", RequireOwnSubmission: false), CancellationToken.None);

        Assert.Equal(ModerateNewProductOutcome.Rejected, result.Outcome);
        Assert.Null(result.ProductId);
        Assert.Equal(ProductSubmissionStatus.Rejected, submission.Status);
        _productRepository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RequireOwnSubmissionAndCallerIsNotSubmitter_ReturnsForbidden()
    {
        var submission = CreateSubmission(ProductSubmissionStatus.Pending);
        _productSubmissionRepository.Setup(r => r.GetByIdAsync(SubmissionId, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ModerateNewProductCommand(SubmissionId, true, "someone-else", null, RequireOwnSubmission: true),
            CancellationToken.None);

        Assert.Equal(ModerateNewProductOutcome.Forbidden, result.Outcome);
        _productRepository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RequireOwnSubmissionAndCallerIsSubmitter_ApprovesAndCreatesProduct()
    {
        var submission = CreateSubmission(ProductSubmissionStatus.Pending);
        _productSubmissionRepository.Setup(r => r.GetByIdAsync(SubmissionId, It.IsAny<CancellationToken>())).ReturnsAsync(submission);
        _productRepository.Setup(r => r.Add(It.IsAny<Product>())).Callback<Product>(p => p.Id = 100);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ModerateNewProductCommand(SubmissionId, true, SubmitterUserId, null, RequireOwnSubmission: true),
            CancellationToken.None);

        Assert.Equal(ModerateNewProductOutcome.Approved, result.Outcome);
        Assert.Equal(100, result.ProductId);
        _productRepository.Verify(r => r.Add(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ApproveWhenBarcodeAlreadyExistsAsProduct_AutoRejectsAsDuplicate()
    {
        var submission = CreateSubmission(ProductSubmissionStatus.Pending);
        _productSubmissionRepository.Setup(r => r.GetByIdAsync(SubmissionId, It.IsAny<CancellationToken>())).ReturnsAsync(submission);
        var existingProduct = new Product
        {
            Id = 55,
            Barcode = new Barcode("1234567890128"),
            Name = "Already catalogued",
            CountryOfOrigin = "TJ"
        };
        _productRepository.Setup(r => r.GetByBarcodeAsync("1234567890128", It.IsAny<CancellationToken>())).ReturnsAsync(existingProduct);

        var handler = CreateHandler();
        var result = await handler.Handle(new ModerateNewProductCommand(SubmissionId, true, AdminUserId, null, RequireOwnSubmission: false), CancellationToken.None);

        Assert.Equal(ModerateNewProductOutcome.DuplicateBarcode, result.Outcome);
        Assert.Equal(55, result.ProductId);
        Assert.Equal(ProductSubmissionStatus.Rejected, submission.Status);
        _productRepository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
    }
}
