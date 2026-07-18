namespace Application.Feedback.Commands.ModerateReport;

public sealed record ModerateReportCommand(int ReportId, bool Resolve, string AdminUserId, string? Reason);
