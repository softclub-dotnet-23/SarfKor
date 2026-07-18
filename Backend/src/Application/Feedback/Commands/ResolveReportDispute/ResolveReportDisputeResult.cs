namespace Application.Feedback.Commands.ResolveReportDispute;

public enum ResolveReportDisputeOutcome
{
    Upheld,
    Dismissed,
    NotFound,
    AlreadyResolved
}

public sealed record ResolveReportDisputeResult(ResolveReportDisputeOutcome Outcome);
