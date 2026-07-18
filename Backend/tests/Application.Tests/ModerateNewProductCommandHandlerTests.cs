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

    private readonly Mock<IProductSubmissionRepository> _productSubmissionRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

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
        SubmittedByUserId = "user-1",
        Status = status,
        CreatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Handle_SubmissionNotFound_ReturnsNotFound()
    {
        _productSubmissionRepository.Setup(r => r.GetByIdAsync(SubmissionId, It.IsAny<CancellationToken>())).ReturnsAsync((ProductSubmission?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new ModerateNewProductCommand(SubmissionId, true, AdminUserId, null), CancellationToken.None);

        Assert.Equal(ModerateNewProductOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_AlreadyModerated_ReturnsAlreadyModerated()
    {
        _productSubmissionRepository
            .Setup(r => r.GetByIdAsync(SubmissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSubmission(ProductSubmissionStatus.Approved));

        var handler = CreateHandler();
        var result = await handler.Handle(new ModerateNewProductCommand(SubmissionId, true, AdminUserId, null), CancellationToken.None);

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
        var result = await handler.Handle(new ModerateNewProductCommand(SubmissionId, true, AdminUserId, null), CancellationToken.None);

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
        var result = await handler.Handle(new ModerateNewProductCommand(SubmissionId, false, AdminUserId, "duplicate"), CancellationToken.None);

        Assert.Equal(ModerateNewProductOutcome.Rejected, result.Outcome);
        Assert.Null(result.ProductId);
        Assert.Equal(ProductSubmissionStatus.Rejected, submission.Status);
        _productRepository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
    }
}
