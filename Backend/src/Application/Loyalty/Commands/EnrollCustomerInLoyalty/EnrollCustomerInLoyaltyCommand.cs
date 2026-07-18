namespace Application.Loyalty.Commands.EnrollCustomerInLoyalty;

public sealed record EnrollCustomerInLoyaltyCommand(int CustomerId, int LoyaltyProgramId, string PerformedByUserId);
