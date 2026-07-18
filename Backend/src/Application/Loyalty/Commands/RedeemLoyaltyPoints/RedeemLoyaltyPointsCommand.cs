namespace Application.Loyalty.Commands.RedeemLoyaltyPoints;

public sealed record RedeemLoyaltyPointsCommand(int LoyaltyAccountId, int Points, string PerformedByUserId);
