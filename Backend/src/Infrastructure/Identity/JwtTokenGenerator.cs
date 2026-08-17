using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Identity;

public sealed class JwtTokenGenerator(IConfiguration configuration)
{
    public (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        // Only added when true -- lets the frontend force the change-password screen even after a
        // page reload (AuthResult.MustChangePassword alone would be lost on reload, since the
        // frontend only ever gets a fresh AuthResult from login/register/refresh, not from decoding
        // an already-stored token). Briefly stale after a successful change until the token
        // naturally rotates (≤15 min) -- ChangePasswordCommandHandler revokes refresh tokens on
        // success, but the still-valid access token keeps this claim; harmless, since re-submitting
        // the (now current) password there just succeeds again.
        if (user.MustChangePassword)
            claims.Add(new Claim("mustChangePassword", "true"));

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
