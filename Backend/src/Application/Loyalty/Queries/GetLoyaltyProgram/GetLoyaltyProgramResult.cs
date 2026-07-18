namespace Application.Loyalty.Queries.GetLoyaltyProgram;

public sealed record GetLoyaltyProgramResult(int? LoyaltyProgramId, decimal? PointsPerCurrencyUnit, decimal? RedemptionRate, bool? IsActive);
