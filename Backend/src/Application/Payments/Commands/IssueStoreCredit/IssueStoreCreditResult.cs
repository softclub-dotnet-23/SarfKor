namespace Application.Payments.Commands.IssueStoreCredit;

public enum IssueStoreCreditOutcome
{
    Issued,
    StoreNotFound,
    CustomerNotFound,
    Forbidden,
    CurrencyMismatch
}

public sealed record IssueStoreCreditResult(IssueStoreCreditOutcome Outcome, decimal? NewBalance);
