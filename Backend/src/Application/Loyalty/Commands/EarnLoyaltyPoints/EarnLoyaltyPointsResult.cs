namespace Application.Loyalty.Commands.EarnLoyaltyPoints;

public enum EarnLoyaltyPointsOutcome
{
    Earned,
    AccountNotFound,
    Forbidden,
    SubscriptionInactive
}

public sealed record EarnLoyaltyPointsResult(EarnLoyaltyPointsOutcome Outcome, int? NewBalance);
