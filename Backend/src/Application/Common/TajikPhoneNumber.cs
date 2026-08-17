using System.Text.RegularExpressions;

namespace Application.Common;

/// <summary>Shared format check for Tajikistan mobile numbers — reused by every command validator
/// that takes a cashier's phone number, so the pattern can't drift between Create/Update. Accepts
/// +992/992/9-digit-local forms with optional spaces/dashes (e.g. "+992 90 123 45 67",
/// "992901234567", "901234567") — Tajik mobile numbers are a 2-digit operator prefix + 7 digits,
/// 9 digits total after the country code.</summary>
public static partial class TajikPhoneNumber
{
    [GeneratedRegex(@"^(\+?992)?[\s-]?\d{2}[\s-]?\d{3}[\s-]?\d{2}[\s-]?\d{2}$")]
    private static partial Regex Pattern();

    public static bool IsValid(string? phoneNumber) => !string.IsNullOrWhiteSpace(phoneNumber) && Pattern().IsMatch(phoneNumber.Trim());
}
