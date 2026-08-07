using System.Security.Cryptography;
using System.Text;

namespace Application.Common;

/// <summary>Shared generate/hash/verify for link-style invitation tokens (store employee invites,
/// and anything similar later) — only the hash is ever persisted, mirroring OtpCode's rule for the
/// same reason: a leaked table must not hand out working invite links. Unlike a 6-digit OTP, a
/// 256-bit random token has far too much entropy for a preimage/rainbow-table attack on its own,
/// so no email-salting is needed here.</summary>
public static class InviteToken
{
    public static string Generate() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static bool Matches(string token, string tokenHash) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Hash(token)), Encoding.UTF8.GetBytes(tokenHash));
}
