namespace Application.Feedback.Queries.GetPendingReports;

public sealed record ReportDto(
    int ReportId,
    string UserId,
    int ProductId,
    int? StoreId,
    string Type,
    string Description,
    DateTimeOffset CreatedAt);

public sealed record GetPendingReportsResult(IReadOnlyList<ReportDto> Reports);
