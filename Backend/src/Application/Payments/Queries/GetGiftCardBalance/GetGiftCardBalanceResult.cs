namespace Application.Payments.Queries.GetGiftCardBalance;

public sealed record GetGiftCardBalanceResult(bool Found, decimal? Balance, string? Currency, bool? IsActive, DateTimeOffset? ExpiresAt);
