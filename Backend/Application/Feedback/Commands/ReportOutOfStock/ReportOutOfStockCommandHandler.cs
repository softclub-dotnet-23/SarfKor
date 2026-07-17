using Application.Abstractions;
using Application.Common;
using Domain.Feedback;

namespace Application.Feedback.Commands.ReportOutOfStock;

public sealed class ReportOutOfStockCommandHandler(
    IReportRepository reportRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ReportOutOfStockCommand, ReportOutOfStockResult>
{
    public async Task<ReportOutOfStockResult> Handle(ReportOutOfStockCommand command, CancellationToken cancellationToken)
    {
        var report = new Report
        {
            UserId = command.UserId,
            ProductId = command.ProductId,
            StoreId = command.StoreId,
            Type = ReportType.OutOfStock,
            Description = command.Description,
            Status = ReportStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        reportRepository.Add(report);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReportOutOfStockResult(report.Id);
    }
}
