using System.Text;

namespace Application.Catalog;

/// <summary>
/// Normalizes a brand name so "Coca-Cola", "Coca Cola", "coca cola", and "Кока-Кола" all reduce to
/// the same key (ADMIN_PROMPT.md §2.8's duplicate-candidate search) — case, whitespace, hyphens
/// stripped, and a best-effort Cyrillic→Latin transliteration for the common visually-confusable
/// letters (Cyrillic "а"/"е"/"о"/"р"/"с"/"х" etc. are literally different Unicode code points from
/// their Latin lookalikes, so a plain case-insensitive compare never catches this on its own).
/// </summary>
public static class BrandNameNormalizer
{
    // Only the letters that are genuinely ambiguous between the two alphabets (visual lookalikes or
    // common informal substitutions) — this is not a full transliteration table, which would
    // over-aggressively merge names that only coincidentally share a Cyrillic letter.
    private static readonly Dictionary<char, char> CyrillicToLatin = new()
    {
        ['а'] = 'a', ['в'] = 'b', ['е'] = 'e', ['к'] = 'k', ['м'] = 'm',
        ['н'] = 'h', ['о'] = 'o', ['р'] = 'p', ['с'] = 'c', ['т'] = 't',
        ['у'] = 'y', ['х'] = 'x', ['ё'] = 'e'
    };

    public static string Normalize(string name)
    {
        var lower = name.ToLowerInvariant();
        var builder = new StringBuilder(lower.Length);

        foreach (var ch in lower)
        {
            if (CyrillicToLatin.TryGetValue(ch, out var latin))
            {
                builder.Append(latin);
                continue;
            }

            if (char.IsWhiteSpace(ch) || ch is '-' or '_' or '.')
                continue;

            if (char.IsLetterOrDigit(ch) || (ch >= 'а' && ch <= 'я'))
                builder.Append(ch);
        }

        return builder.ToString();
    }
}
