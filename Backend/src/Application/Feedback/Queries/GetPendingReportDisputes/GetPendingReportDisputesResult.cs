namespace Application.Feedback.Queries.GetPendingReportDisputes;

public sealed record ReportDisputeDto(int DisputeId, int ReportId, string DisputedByUserId, string Reason, DateTimeOffset CreatedAt);

public sealed record GetPendingReportDisputesResult(IReadOnlyList<ReportDisputeDto> Disputes);
