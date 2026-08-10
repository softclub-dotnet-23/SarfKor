namespace Application.Payments.Commands.IssueGiftCard;

public enum IssueGiftCardOutcome
{
    Issued,
    StoreNotFound,
    Forbidden,
    SubscriptionInactive
}

public sealed record IssueGiftCardResult(IssueGiftCardOutcome Outcome, int? GiftCardId, string? Code);
