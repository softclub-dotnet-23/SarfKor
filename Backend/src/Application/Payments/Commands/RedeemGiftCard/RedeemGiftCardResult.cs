namespace Application.Payments.Commands.RedeemGiftCard;

public enum RedeemGiftCardOutcome
{
    Redeemed,
    NotFound,
    Inactive,
    Expired,
    InsufficientBalance,
    CurrencyMismatch,
    Forbidden
}

public sealed record RedeemGiftCardResult(RedeemGiftCardOutcome Outcome, decimal? RemainingBalance);
