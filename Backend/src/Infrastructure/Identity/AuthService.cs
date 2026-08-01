using Application.Abstractions;
using Domain.Identity;
using Domain.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    JwtTokenGenerator tokenGenerator,
    IRefreshTokenRepository refreshTokenRepository,
    ISecurityEventRepository securityEventRepository,
    IUnitOfWork unitOfWork) : IAuthService
{
    private const string DefaultRole = "User";

    public async Task<AuthResult?> RegisterAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser { UserName = email, Email = email };
        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
            return null;

        await userManager.AddToRoleAsync(user, DefaultRole);

        var result = await IssueTokenPairAsync(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<AuthResult?> LoginAsync(string email, string password, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        // Account lockout (on top of the IP-based "login" rate-limit policy) — the rate limit
        // alone is bypassable via distributed/proxied attempts; this caps guesses per-account
        // regardless of source IP. A locked-out account fails the same way as a wrong password,
        // so this check never leaks which accounts exist or are currently locked.
        if (user is not null && await userManager.IsLockedOutAsync(user))
        {
            securityEventRepository.Add(new SecurityEvent
            {
                UserId = user.Id,
                Type = SecurityEventType.LoginFailed,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                OccurredAt = DateTimeOffset.UtcNow
            });
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        var succeeded = user is not null && await userManager.CheckPasswordAsync(user, password);

        if (user is not null)
        {
            if (succeeded)
                await userManager.ResetAccessFailedCountAsync(user);
            else
                await userManager.AccessFailedAsync(user);
        }

        // Recorded for both outcomes — a string of LoginFailed events for the same account is
        // exactly the anomaly signal CLAUDE.md §10 asks for ("алерты на аномальные паттерны").
        // user can be null on a failed attempt (unknown email) — UserId falls back to the
        // attempted email so the event is still attributable to something searchable.
        securityEventRepository.Add(new SecurityEvent
        {
            UserId = user?.Id ?? email,
            Type = succeeded ? SecurityEventType.LoginSucceeded : SecurityEventType.LoginFailed,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            OccurredAt = DateTimeOffset.UtcNow
        });

        if (!succeeded)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        var result = await IssueTokenPairAsync(user!);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<AuthResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var existingToken = await refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken);
        if (existingToken is null || existingToken.RevokedAt is not null || existingToken.ExpiresAt < DateTimeOffset.UtcNow)
            return null;

        var user = await userManager.FindByIdAsync(existingToken.UserId);
        if (user is null)
            return null;

        var result = await IssueTokenPairAsync(user);

        existingToken.RevokedAt = DateTimeOffset.UtcNow;
        existingToken.ReplacedByToken = result.RefreshToken;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task AssignRoleAsync(string userId, string role, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || await userManager.IsInRoleAsync(user, role))
            return;

        await userManager.AddToRoleAsync(user, role);
    }

    public async Task RemoveFromRoleAsync(string userId, string role, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !await userManager.IsInRoleAsync(user, role))
            return;

        await userManager.RemoveFromRoleAsync(user, role);
    }

    public async Task<string?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user?.Id;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetEmailsByUserIdsAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken)
    {
        var rows = await userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(cancellationToken);

        return rows.Where(r => r.Email is not null).ToDictionary(r => r.Id, r => r.Email!);
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : await userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return false;

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            return false;

        // A stolen refresh token must die the moment the real owner takes back the account.
        await refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        securityEventRepository.Add(new SecurityEvent
        {
            UserId = user.Id,
            Type = SecurityEventType.PasswordChanged,
            IpAddress = null,
            UserAgent = null,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<AuthResult> IssueTokenPairAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = tokenGenerator.GenerateAccessToken(user, roles);

        var refreshTokenValue = Guid.NewGuid().ToString("N");
        refreshTokenRepository.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow
        });

        return new AuthResult(user.Id, accessToken, refreshTokenValue, expiresAt);
    }
}
