namespace Application.Payments.Commands.IssueGiftCard;

public enum IssueGiftCardOutcome
{
    Issued,
    StoreNotFound,
    Forbidden
}

public sealed record IssueGiftCardResult(IssueGiftCardOutcome Outcome, int? GiftCardId, string? Code);
