using Application.Abstractions;
using Application.Products.Commands.RecordScan;
using Domain.Analytics;
using Moq;

namespace Application.Tests;

public class RecordScanCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IScanRepository> _scanRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RecordScanCommandHandler CreateHandler() => new(_productRepository.Object, _scanRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_ProductDoesNotExist_ReturnsProductNotFound()
    {
        _productRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new RecordScanCommand(1, "user-1", 2), CancellationToken.None);

        Assert.Equal(RecordScanOutcome.ProductNotFound, result.Outcome);
        _scanRepository.Verify(r => r.Add(It.IsAny<Scan>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AnonymousScan_RecordsWithNullUserId()
    {
        _productRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _scanRepository.Setup(r => r.Add(It.IsAny<Scan>())).Callback<Scan>(s => s.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(new RecordScanCommand(1, null, null), CancellationToken.None);

        Assert.Equal(RecordScanOutcome.Recorded, result.Outcome);
        Assert.Equal(1, result.ScanId);
        _scanRepository.Verify(r => r.Add(It.Is<Scan>(s => s.UserId == null && s.ProductId == 1)), Times.Once);
    }
}
