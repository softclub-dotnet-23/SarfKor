using Application.Abstractions;
using Application.Common;
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
    private static readonly TimeSpan EmailConfirmationCodeLifespan = TimeSpan.FromMinutes(15);
    private const int MaxEmailConfirmationAttempts = 5;

    public async Task<RegisterAccountResult> RegisterAsync(string email, string password, bool emailPreVerified, CancellationToken cancellationToken)
    {
        // A still-unconfirmed account from an earlier register attempt (wrong code, email never
        // arrived, etc.) isn't a real duplicate — without this, someone who mistypes their code
        // would be permanently locked out with no way to get a fresh one.
        var existingUnconfirmed = await userManager.FindByEmailAsync(email);
        if (existingUnconfirmed is not null && !existingUnconfirmed.EmailConfirmed && !emailPreVerified)
            return await ResendConfirmationCodeAsync(existingUnconfirmed, cancellationToken);

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = emailPreVerified };
        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            // UserName == email here, so a duplicate email surfaces as a duplicate-username error —
            // RequireUniqueEmail isn't configured, but username uniqueness is always enforced.
            var emailAlreadyRegistered = createResult.Errors.Any(e => e.Code is "DuplicateUserName" or "DuplicateEmail");
            return new RegisterAccountResult(null, emailAlreadyRegistered);
        }

        await userManager.AddToRoleAsync(user, DefaultRole);

        // Self-registration: no tokens yet — the account can't log in until the code is confirmed
        // (LoginAsync/ConfirmEmailAsync both check EmailConfirmed).
        if (!emailPreVerified)
            return await ResendConfirmationCodeAsync(user, cancellationToken);

        var result = await IssueTokenPairAsync(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RegisterAccountResult(result, false);
    }

    public async Task<ConfirmEmailResult> ConfirmEmailAsync(string email, string code, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        // Covers "no such account," "already confirmed" (code fields cleared on success), and
        // "code expired" with one indistinguishable answer — no separate branch needed.
        if (user is null || user.EmailConfirmationCodeHash is null || user.EmailConfirmationCodeExpiresAt < DateTimeOffset.UtcNow)
            return new ConfirmEmailResult(null, InvalidOrExpiredCode: true, TooManyAttempts: false);

        if (user.EmailConfirmationAttempts >= MaxEmailConfirmationAttempts)
            return new ConfirmEmailResult(null, InvalidOrExpiredCode: false, TooManyAttempts: true);

        if (!OtpCode.Matches(email, code, user.EmailConfirmationCodeHash))
        {
            user.EmailConfirmationAttempts++;
            await userManager.UpdateAsync(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new ConfirmEmailResult(null, InvalidOrExpiredCode: true, TooManyAttempts: false);
        }

        user.EmailConfirmed = true;
        user.EmailConfirmationCodeHash = null;
        user.EmailConfirmationCodeExpiresAt = null;
        user.EmailConfirmationAttempts = 0;
        await userManager.UpdateAsync(user);

        var result = await IssueTokenPairAsync(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ConfirmEmailResult(result, InvalidOrExpiredCode: false, TooManyAttempts: false);
    }

    public async Task<LoginAccountResult> LoginAsync(string email, string password, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
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
            return new LoginAccountResult(null, false);
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
        // Only logged when a real user exists; unknown-email attempts are already blocked by
        // the IP-level "login" rate-limit policy, and there is no valid FK target to attach an
        // event to (UserId is a FK → AspNetUsers.Id, not a free-form string).
        if (user is not null)
        {
            securityEventRepository.Add(new SecurityEvent
            {
                UserId = user.Id,
                Type = succeeded ? SecurityEventType.LoginSucceeded : SecurityEventType.LoginFailed,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                OccurredAt = DateTimeOffset.UtcNow
            });
        }

        if (!succeeded)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new LoginAccountResult(null, false);
        }

        // Correct password, but the registration OTP was never confirmed — the password check
        // above already ran (not skipped ahead of it), so this can't be used to probe whether an
        // email exists via timing/behavior differences.
        if (!user!.EmailConfirmed)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new LoginAccountResult(null, true);
        }

        var result = await IssueTokenPairAsync(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new LoginAccountResult(result, false);
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

    public async Task<string?> GeneratePasswordResetCodeAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        // Deliberately not gated on EmailConfirmed — someone who never finished registering can
        // still have simply forgotten the password they set, and confirming just needs the code,
        // not the old password, so there's no reason to block this on that unrelated flag.
        return user is null ? null : await IssueEmailVerificationCodeAsync(user, cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || user.EmailConfirmationCodeHash is null || user.EmailConfirmationCodeExpiresAt < DateTimeOffset.UtcNow)
            return false;

        if (user.EmailConfirmationAttempts >= MaxEmailConfirmationAttempts)
            return false;

        if (!OtpCode.Matches(email, code, user.EmailConfirmationCodeHash))
        {
            user.EmailConfirmationAttempts++;
            await userManager.UpdateAsync(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return false;
        }

        user.EmailConfirmationCodeHash = null;
        user.EmailConfirmationCodeExpiresAt = null;
        user.EmailConfirmationAttempts = 0;
        await userManager.UpdateAsync(user);

        // No old password needed — the code above is what already proved this request is
        // legitimate, same as ConfirmEmailAsync using the code instead of the account's password.
        await userManager.RemovePasswordAsync(user);
        var addResult = await userManager.AddPasswordAsync(user, newPassword);
        if (!addResult.Succeeded)
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

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new InvalidOperationException($"User {userId} not found.");

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            return false;

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

    private async Task<RegisterAccountResult> ResendConfirmationCodeAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        new(null, false, RequiresEmailConfirmation: true, EmailConfirmationCode: await IssueEmailVerificationCodeAsync(user, cancellationToken));

    private async Task<string> IssueEmailVerificationCodeAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var code = OtpCode.Generate();
        user.EmailConfirmationCodeHash = OtpCode.Hash(user.Email!, code);
        user.EmailConfirmationCodeExpiresAt = DateTimeOffset.UtcNow.Add(EmailConfirmationCodeLifespan);
        user.EmailConfirmationAttempts = 0;
        await userManager.UpdateAsync(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return code;
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
