namespace Application.Feedback.Commands.RaiseReportDispute;

public enum RaiseReportDisputeOutcome
{
    Raised,
    ReportNotFound,
    Forbidden
}

public sealed record RaiseReportDisputeResult(RaiseReportDisputeOutcome Outcome, int? DisputeId);
