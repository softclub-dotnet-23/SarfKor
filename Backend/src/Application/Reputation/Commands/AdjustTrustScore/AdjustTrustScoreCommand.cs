namespace Application.Reputation.Commands.AdjustTrustScore;

public sealed record AdjustTrustScoreCommand(string UserId, double Delta, string Reason, string PerformedByAdminUserId, string? PerformedByIpAddress = null);
