namespace Application.Loyalty.Commands.EnrollCustomerInLoyalty;

public enum EnrollCustomerInLoyaltyOutcome
{
    Enrolled,
    AlreadyEnrolled,
    CustomerNotFound,
    ProgramNotFound,
    Forbidden
}

public sealed record EnrollCustomerInLoyaltyResult(EnrollCustomerInLoyaltyOutcome Outcome, int? LoyaltyAccountId);
