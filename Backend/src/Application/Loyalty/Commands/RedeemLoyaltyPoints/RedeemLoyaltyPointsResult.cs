namespace Application.Loyalty.Commands.RedeemLoyaltyPoints;

public enum RedeemLoyaltyPointsOutcome
{
    Redeemed,
    AccountNotFound,
    Forbidden,
    InsufficientPoints,
    SubscriptionInactive
}

public sealed record RedeemLoyaltyPointsResult(RedeemLoyaltyPointsOutcome Outcome, int? NewBalance);
