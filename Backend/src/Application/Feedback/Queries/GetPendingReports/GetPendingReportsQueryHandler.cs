using Application.Abstractions;
using Application.Common;

namespace Application.Feedback.Queries.GetPendingReports;

public sealed class GetPendingReportsQueryHandler(IReportRepository reportRepository)
    : IQueryHandler<GetPendingReportsQuery, GetPendingReportsResult>
{
    public async Task<GetPendingReportsResult> Handle(GetPendingReportsQuery query, CancellationToken cancellationToken)
    {
        var reports = await reportRepository.GetPendingAsync(cancellationToken);
        var dtos = reports
            .Select(r => new ReportDto(r.Id, r.UserId, r.ProductId, r.StoreId, r.Type.ToString(), r.Description, r.CreatedAt))
            .ToList();
        return new GetPendingReportsResult(dtos);
    }
}
