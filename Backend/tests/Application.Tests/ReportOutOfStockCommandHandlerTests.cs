using Application.Abstractions;
using Application.Feedback.Commands.ReportOutOfStock;
using Domain.Feedback;
using Domain.Pricing;
using Domain.Reputation;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class ReportOutOfStockCommandHandlerTests
{
    private readonly Mock<IReportRepository> _reportRepository = new();
    private readonly Mock<IPriceEntryRepository> _priceEntryRepository = new();
    private readonly Mock<IContributorTrustScoreRepository> _trustScoreRepository = new();
    private readonly Mock<IContributorTrustScoreAdjustmentRepository> _trustScoreAdjustmentRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ReportOutOfStockCommandHandler CreateHandler() => new(
        _reportRepository.Object, _priceEntryRepository.Object, _trustScoreRepository.Object,
        _trustScoreAdjustmentRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_CreatesReport_AndReturnsItsId()
    {
        Report? added = null;
        _reportRepository.Setup(r => r.Add(It.IsAny<Report>())).Callback<Report>(r =>
        {
            r.Id = 42;
            added = r;
        });

        var handler = CreateHandler();
        var result = await handler.Handle(new ReportOutOfStockCommand("user-1", 5, 2, "Empty shelf"), CancellationToken.None);

        Assert.Equal(42, result.ReportId);
        Assert.NotNull(added);
        Assert.Equal(ReportType.OutOfStock, added!.Type);
        Assert.Equal(5, added.ProductId);
        Assert.Equal(2, added.StoreId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductHasCurrentPriceAuthor_PenalizesAuthorsTrustScore()
    {
        _priceEntryRepository
            .Setup(r => r.GetLatestForStoreAsync(5, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceEntry { ProductId = 5, StoreId = 2, Price = new Money(10, "TJS"), SubmittedByUserId = "author-1", RecordedAt = DateTimeOffset.UtcNow });
        _trustScoreRepository.Setup(r => r.GetByUserIdAsync("author-1", It.IsAny<CancellationToken>())).ReturnsAsync((ContributorTrustScore?)null);

        var handler = CreateHandler();
        await handler.Handle(new ReportOutOfStockCommand("user-1", 5, 2, "Empty shelf"), CancellationToken.None);

        _trustScoreRepository.Verify(r => r.Add(It.Is<ContributorTrustScore>(t => t.UserId == "author-1")), Times.Once);
        _trustScoreAdjustmentRepository.Verify(r => r.Add(It.Is<ContributorTrustScoreAdjustment>(a => a.UserId == "author-1" && !a.IsManual && a.Delta < 0)), Times.Once);
    }

    [Fact]
    public async Task Handle_NoStoreId_DoesNotTouchTrustScore()
    {
        var handler = CreateHandler();
        await handler.Handle(new ReportOutOfStockCommand("user-1", 5, null, "No store"), CancellationToken.None);

        _priceEntryRepository.Verify(r => r.GetLatestForStoreAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _trustScoreRepository.Verify(r => r.Add(It.IsAny<ContributorTrustScore>()), Times.Never);
    }
}
