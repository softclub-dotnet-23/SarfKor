namespace Application.Loyalty.Commands.EarnLoyaltyPoints;

public enum EarnLoyaltyPointsOutcome
{
    Earned,
    AccountNotFound,
    Forbidden
}

public sealed record EarnLoyaltyPointsResult(EarnLoyaltyPointsOutcome Outcome, int? NewBalance);
