namespace Application.Payments.Commands.IssueGiftCard;

public sealed record IssueGiftCardCommand(decimal Amount, string Currency, DateTimeOffset? ExpiresAt);
