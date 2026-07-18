namespace Application.Loyalty.Commands.CreateLoyaltyProgram;

public sealed record CreateLoyaltyProgramCommand(int StoreId, decimal PointsPerCurrencyUnit, decimal RedemptionRate, string PerformedByUserId);
