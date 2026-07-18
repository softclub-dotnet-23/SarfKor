namespace Application.Feedback.Commands.ModerateReport;

public enum ModerateReportOutcome
{
    Resolved,
    Rejected,
    NotFound,
    AlreadyModerated
}

public sealed record ModerateReportResult(ModerateReportOutcome Outcome);
