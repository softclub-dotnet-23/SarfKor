namespace Application.Payments.Commands.RedeemStoreCredit;

public enum RedeemStoreCreditOutcome
{
    Redeemed,
    StoreNotFound,
    Forbidden,
    NoCreditOnFile,
    InsufficientBalance,
    CurrencyMismatch
}

public sealed record RedeemStoreCreditResult(RedeemStoreCreditOutcome Outcome, decimal? NewBalance);
