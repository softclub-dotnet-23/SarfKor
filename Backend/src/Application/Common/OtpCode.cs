using System.Security.Cryptography;
using System.Text;

namespace Application.Common;

/// <summary>Shared generate/hash/verify logic for short numeric confirmation codes (e.g. the
/// admin-invites-a-store-owner flow), so the hashing rule can't drift between the two handlers
/// that need it.</summary>
public static class OtpCode
{
    public static string Generate() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    // Salted with the email: a bare SHA-256 of a 6-digit code has only 10^6 preimages and would
    // fall instantly to an offline lookup table if this table ever leaked — unlike a password
    // hash, this doesn't need to be slow, just not a trivial dictionary lookup.
    public static string Hash(string email, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{email.ToLowerInvariant()}:{code}")));

    public static bool Matches(string email, string code, string codeHash) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(email, code)),
            Encoding.UTF8.GetBytes(codeHash));
}
