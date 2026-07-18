namespace Application.Loyalty.Commands.EarnLoyaltyPoints;

public sealed record EarnLoyaltyPointsCommand(int LoyaltyAccountId, int Points, int? SaleTransactionId, string PerformedByUserId);
