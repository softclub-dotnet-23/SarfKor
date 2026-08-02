namespace Application.Common;

/// <summary>The currencies the platform actually deals in — Tajikistan (TJS) plus the currencies
/// storefronts commonly price/report in. Not a full ISO-4217 list on purpose: allowing arbitrary
/// 3-letter codes let VAL-11-style "XXX"/"ABC" garbage into Money.Currency with no rejection.</summary>
public static class SupportedCurrencies
{
    public static readonly IReadOnlySet<string> Codes = new HashSet<string>(StringComparer.Ordinal)
    {
        "TJS", "USD", "RUB", "EUR"
    };

    public static bool IsSupported(string? currency) => currency is not null && Codes.Contains(currency);
}
