namespace Application.Payments.Commands.RedeemStoreCredit;

public enum RedeemStoreCreditOutcome
{
    Redeemed,
    StoreNotFound,
    Forbidden,
    NoCreditOnFile,
    InsufficientBalance,
    CurrencyMismatch,
    SubscriptionInactive
}

public sealed record RedeemStoreCreditResult(RedeemStoreCreditOutcome Outcome, decimal? NewBalance);
