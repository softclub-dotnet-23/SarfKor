namespace Application.Feedback.Commands.RaiseReportDispute;

public sealed record RaiseReportDisputeCommand(int ReportId, string DisputedByUserId, string Reason);
