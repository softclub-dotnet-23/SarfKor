namespace Application.Loyalty.Commands.CreateLoyaltyProgram;

public enum CreateLoyaltyProgramOutcome
{
    Created,
    StoreNotFound,
    Forbidden,
    AlreadyExists
}

public sealed record CreateLoyaltyProgramResult(CreateLoyaltyProgramOutcome Outcome, int? LoyaltyProgramId);
