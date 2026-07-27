using Domain.Feedback;

namespace Application.Feedback.Queries.GetPendingReports;

public sealed record ReportDto(
    int ReportId,
    string UserId,
    int ProductId,
    int? StoreId,
    ReportType Type,
    string Description,
    DateTimeOffset CreatedAt);

public sealed record GetPendingReportsResult(IReadOnlyList<ReportDto> Reports);
