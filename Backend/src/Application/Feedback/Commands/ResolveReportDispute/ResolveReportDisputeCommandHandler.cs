using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Feedback;

namespace Application.Feedback.Commands.ResolveReportDispute;

public sealed class ResolveReportDisputeCommandHandler(
    IReportDisputeRepository reportDisputeRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ResolveReportDisputeCommand, ResolveReportDisputeResult>
{
    public async Task<ResolveReportDisputeResult> Handle(ResolveReportDisputeCommand command, CancellationToken cancellationToken)
    {
        var dispute = await reportDisputeRepository.GetByIdAsync(command.DisputeId, cancellationToken);
        if (dispute is null)
            return new ResolveReportDisputeResult(ResolveReportDisputeOutcome.NotFound);

        if (dispute.Status != ReportDisputeStatus.Pending)
            return new ResolveReportDisputeResult(ResolveReportDisputeOutcome.AlreadyResolved);

        dispute.Status = command.Uphold ? ReportDisputeStatus.Upheld : ReportDisputeStatus.Dismissed;

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.AdminUserId,
            Action = command.Uphold ? "ReportDispute.Upheld" : "ReportDispute.Dismissed",
            EntityType = nameof(ReportDispute),
            EntityId = dispute.Id,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ResolveReportDisputeResult(command.Uphold ? ResolveReportDisputeOutcome.Upheld : ResolveReportDisputeOutcome.Dismissed);
    }
}
