using Application.Abstractions;
using Application.Common;
using Domain.Feedback;

namespace Application.Feedback.Commands.RaiseReportDispute;

public sealed class RaiseReportDisputeCommandHandler(
    IReportRepository reportRepository,
    IStoreRepository storeRepository,
    IReportDisputeRepository reportDisputeRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RaiseReportDisputeCommand, RaiseReportDisputeResult>
{
    public async Task<RaiseReportDisputeResult> Handle(RaiseReportDisputeCommand command, CancellationToken cancellationToken)
    {
        var report = await reportRepository.GetByIdAsync(command.ReportId, cancellationToken);
        if (report is null)
            return new RaiseReportDisputeResult(RaiseReportDisputeOutcome.ReportNotFound, null);

        // Only the owner of the store a report is about can dispute it — a report with no store
        // (e.g. about the product listing generally) has no legitimate disputant.
        if (report.StoreId is null)
            return new RaiseReportDisputeResult(RaiseReportDisputeOutcome.Forbidden, null);

        var store = await storeRepository.GetByIdAsync(report.StoreId.Value, cancellationToken);
        if (store is null || store.OwnerUserId != command.DisputedByUserId)
            return new RaiseReportDisputeResult(RaiseReportDisputeOutcome.Forbidden, null);

        var dispute = new ReportDispute
        {
            ReportId = command.ReportId,
            DisputedByUserId = command.DisputedByUserId,
            Reason = command.Reason,
            Status = ReportDisputeStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        reportDisputeRepository.Add(dispute);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RaiseReportDisputeResult(RaiseReportDisputeOutcome.Raised, dispute.Id);
    }
}
