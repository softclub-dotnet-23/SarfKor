using Application.Abstractions;
using Application.Feedback.Commands.ReportOutOfStock;
using Domain.Feedback;
using Moq;

namespace Application.Tests;

public class ReportOutOfStockCommandHandlerTests
{
    private readonly Mock<IReportRepository> _reportRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Handle_CreatesPendingReport_AndReturnsItsId()
    {
        Report? added = null;
        _reportRepository.Setup(r => r.Add(It.IsAny<Report>())).Callback<Report>(r =>
        {
            r.Id = 42;
            added = r;
        });

        var handler = new ReportOutOfStockCommandHandler(_reportRepository.Object, _unitOfWork.Object);
        var result = await handler.Handle(new ReportOutOfStockCommand("user-1", 5, 2, "Empty shelf"), CancellationToken.None);

        Assert.Equal(42, result.ReportId);
        Assert.NotNull(added);
        Assert.Equal(ReportType.OutOfStock, added!.Type);
        Assert.Equal(ReportStatus.Pending, added.Status);
        Assert.Equal(5, added.ProductId);
        Assert.Equal(2, added.StoreId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
