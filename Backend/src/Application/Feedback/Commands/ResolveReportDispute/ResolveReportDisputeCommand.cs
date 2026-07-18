namespace Application.Feedback.Commands.ResolveReportDispute;

public sealed record ResolveReportDisputeCommand(int DisputeId, bool Uphold, string AdminUserId);
