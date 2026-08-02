namespace Domain.ValueObjects;

/// <summary>
/// Validates on both direct construction and `with`-expressions (the init accessors run either
/// way) — a bare positional record let negative amounts and malformed currency codes through
/// silently, which is exactly the shape of state that should never be constructible at all.
/// </summary>
public sealed record Money
{
    private readonly decimal _amount;
    private readonly string _currency = "";

    public decimal Amount
    {
        get => _amount;
        init => _amount = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Amount), value, "Money amount cannot be negative.");
    }

    public string Currency
    {
        get => _currency;
        init => _currency = value is { Length: 3 }
            ? value
            : throw new ArgumentException("Currency must be a 3-letter code.", nameof(Currency));
    }

    public Money(decimal Amount, string Currency)
    {
        this.Amount = Amount;
        this.Currency = Currency;
    }
}
