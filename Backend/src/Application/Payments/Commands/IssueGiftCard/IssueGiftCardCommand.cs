namespace Application.Payments.Commands.IssueGiftCard;

public sealed record IssueGiftCardCommand(int StoreId, string PerformedByUserId, decimal Amount, string Currency, DateTimeOffset? ExpiresAt);
