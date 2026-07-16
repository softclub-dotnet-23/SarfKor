namespace Domain.ValueObjects;

public sealed record PaymentToken(string Provider, string Token);
