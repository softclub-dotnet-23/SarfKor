namespace Application.Payments.Commands.IssueStoreCredit;

public enum IssueStoreCreditOutcome
{
    Issued,
    StoreNotFound,
    CustomerNotFound,
    Forbidden
}

public sealed record IssueStoreCreditResult(IssueStoreCreditOutcome Outcome, decimal? NewBalance);
