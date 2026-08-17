using System.Security.Cryptography;

namespace Application.Common;

/// <summary>Generates a one-time password for an account the caller is setting up on someone else's
/// behalf without ever asking that person to type one in first — e.g. a store owner creating a
/// cashier login on the spot (CreateCashierAccountCommandHandler). Unlike an invite link, this
/// password is real and usable immediately, so it's shown to the caller exactly once in the response
/// and never persisted or logged anywhere in plaintext; only ASP.NET Identity's own hash of it ever
/// reaches the database.</summary>
public static class GeneratedPassword
{
    // No 0/O/1/l/I — this password gets read aloud or typed off a screen by a store owner handing it
    // to a cashier, and those pairs are the single biggest source of "the password doesn't work"
    // support requests for any human-transcribed credential.
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghjkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Special = "!@#$%&*?";

    public static string Generate()
    {
        // One guaranteed char from each class (Identity's default password policy requires all
        // four), then fill the rest from the combined pool, then shuffle -- otherwise the first four
        // characters would always be upper/lower/digit/special in that fixed order.
        var all = Upper + Lower + Digits + Special;
        var chars = new List<char>
        {
            Pick(Upper), Pick(Lower), Pick(Digits), Pick(Special),
        };
        for (var i = chars.Count; i < 12; i++)
            chars.Add(Pick(all));

        // Fisher-Yates using the same CSPRNG, not Random -- this password grants real account access.
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(0, i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
    }

    private static char Pick(string pool) => pool[RandomNumberGenerator.GetInt32(0, pool.Length)];
}
