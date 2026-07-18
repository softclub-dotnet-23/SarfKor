using Application.Abstractions;
using Application.Common;

namespace Application.Feedback.Queries.GetPendingReportDisputes;

public sealed class GetPendingReportDisputesQueryHandler(IReportDisputeRepository reportDisputeRepository)
    : IQueryHandler<GetPendingReportDisputesQuery, GetPendingReportDisputesResult>
{
    public async Task<GetPendingReportDisputesResult> Handle(GetPendingReportDisputesQuery query, CancellationToken cancellationToken)
    {
        var disputes = await reportDisputeRepository.GetPendingAsync(cancellationToken);
        var dtos = disputes.Select(d => new ReportDisputeDto(d.Id, d.ReportId, d.DisputedByUserId, d.Reason, d.CreatedAt)).ToList();
        return new GetPendingReportDisputesResult(dtos);
    }
}
