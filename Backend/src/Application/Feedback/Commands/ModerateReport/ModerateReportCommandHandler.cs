using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Feedback;

namespace Application.Feedback.Commands.ModerateReport;

public sealed class ModerateReportCommandHandler(
    IReportRepository reportRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ModerateReportCommand, ModerateReportResult>
{
    public async Task<ModerateReportResult> Handle(ModerateReportCommand command, CancellationToken cancellationToken)
    {
        var report = await reportRepository.GetByIdAsync(command.ReportId, cancellationToken);
        if (report is null)
            return new ModerateReportResult(ModerateReportOutcome.NotFound);

        if (report.Status != ReportStatus.Pending)
            return new ModerateReportResult(ModerateReportOutcome.AlreadyModerated);

        report.Status = command.Resolve ? ReportStatus.Resolved : ReportStatus.Rejected;
        report.ResolvedByAdminUserId = command.AdminUserId;
        report.ResolvedAt = DateTimeOffset.UtcNow;

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.AdminUserId,
            Action = command.Resolve ? "Report.Resolved" : "Report.Rejected",
            EntityType = nameof(Report),
            EntityId = report.Id,
            Details = command.Reason,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ModerateReportResult(command.Resolve ? ModerateReportOutcome.Resolved : ModerateReportOutcome.Rejected);
    }
}
