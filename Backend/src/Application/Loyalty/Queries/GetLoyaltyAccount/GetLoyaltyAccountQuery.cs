namespace Application.Loyalty.Queries.GetLoyaltyAccount;

public sealed record GetLoyaltyAccountQuery(int CustomerId, int LoyaltyProgramId, string RequestedByUserId);
