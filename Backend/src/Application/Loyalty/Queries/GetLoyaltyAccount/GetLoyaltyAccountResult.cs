namespace Application.Loyalty.Queries.GetLoyaltyAccount;

public enum GetLoyaltyAccountOutcome
{
    Found,
    NotFound,
    Forbidden
}

public sealed record GetLoyaltyAccountResult(GetLoyaltyAccountOutcome Outcome, int? LoyaltyAccountId, int? PointsBalance);
