using Application.Abstractions;
using Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    JwtTokenGenerator tokenGenerator,
    IRefreshTokenRepository refreshTokenRepository,
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

    public async Task<AuthResult?> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
            return null;

        var result = await IssueTokenPairAsync(user);
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
