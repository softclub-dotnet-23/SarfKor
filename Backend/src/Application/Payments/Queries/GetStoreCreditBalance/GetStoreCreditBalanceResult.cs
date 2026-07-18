namespace Application.Payments.Queries.GetStoreCreditBalance;

public enum GetStoreCreditBalanceOutcome
{
    Found,
    StoreNotFound,
    Forbidden
}

public sealed record GetStoreCreditBalanceResult(GetStoreCreditBalanceOutcome Outcome, decimal? Balance, string? Currency);
