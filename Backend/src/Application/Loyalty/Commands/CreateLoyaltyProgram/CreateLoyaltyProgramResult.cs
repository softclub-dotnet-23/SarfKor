namespace Application.Loyalty.Commands.CreateLoyaltyProgram;

public enum CreateLoyaltyProgramOutcome
{
    Created,
    StoreNotFound,
    Forbidden,
    AlreadyExists,
    SubscriptionInactive
}

public sealed record CreateLoyaltyProgramResult(CreateLoyaltyProgramOutcome Outcome, int? LoyaltyProgramId);
