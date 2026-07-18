namespace Application.Loyalty.Commands.RedeemLoyaltyPoints;

public enum RedeemLoyaltyPointsOutcome
{
    Redeemed,
    AccountNotFound,
    Forbidden,
    InsufficientPoints
}

public sealed record RedeemLoyaltyPointsResult(RedeemLoyaltyPointsOutcome Outcome, int? NewBalance);
