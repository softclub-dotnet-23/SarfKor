namespace Application.Loyalty.Commands.EnrollCustomerInLoyalty;

public enum EnrollCustomerInLoyaltyOutcome
{
    Enrolled,
    AlreadyEnrolled,
    CustomerNotFound,
    ProgramNotFound,
    Forbidden,
    SubscriptionInactive
}

public sealed record EnrollCustomerInLoyaltyResult(EnrollCustomerInLoyaltyOutcome Outcome, int? LoyaltyAccountId);
