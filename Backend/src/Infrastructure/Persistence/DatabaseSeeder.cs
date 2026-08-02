using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

/// <summary>
/// Seeds a fixed set of development accounts so every clean database has logins
/// ready to test all three roles without manual setup.
///
/// Only runs when ASPNETCORE_ENVIRONMENT == "Development" (enforced in Program.cs).
/// Every operation is idempotent: accounts already at the right state are skipped.
/// </summary>
public static class DatabaseSeeder
{
    private record DevAccount(string Email, string Password, string Role);

    private static readonly DevAccount[] DevAccounts =
    [
        new("admin@sarfkor.tj",   "Admin123!",   "Admin"),
        new("partner@sarfkor.tj", "Partner123!", "StorePartner"),
        new("user@sarfkor.tj",    "User123!",    "User"),
    ];

    public static async Task SeedDevAccountsAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        foreach (var account in DevAccounts)
        {
            var user = await userManager.FindByEmailAsync(account.Email);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = account.Email,
                    Email = account.Email,
                    // Skip the email-confirmation flow for dev accounts — they need to be
                    // immediately usable without an SMTP server running locally.
                    EmailConfirmed = true,
                };

                var createResult = await userManager.CreateAsync(user, account.Password);
                if (!createResult.Succeeded)
                {
                    logger.LogError(
                        "Failed to seed dev account {Email}: {Errors}",
                        account.Email,
                        string.Join("; ", createResult.Errors.Select(e => e.Description)));
                    continue;
                }

                logger.LogInformation("Seeded dev account {Email}", account.Email);
            }
            else
            {
                // Account exists — make sure it is confirmed and the password matches.
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await userManager.UpdateAsync(user);
                }

                // Reset the password unconditionally so the dev password is always current
                // even if someone changed it or the account pre-existed with a different one.
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await userManager.ResetPasswordAsync(user, resetToken, account.Password);
                if (!resetResult.Succeeded)
                {
                    logger.LogError(
                        "Failed to reset password for dev account {Email}: {Errors}",
                        account.Email,
                        string.Join("; ", resetResult.Errors.Select(e => e.Description)));
                }
                else
                {
                    logger.LogInformation("Dev account {Email} password is up to date", account.Email);
                }
            }

            // Ensure the correct role is assigned (add it if missing, never remove existing roles).
            if (!await userManager.IsInRoleAsync(user, account.Role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, account.Role);
                if (!roleResult.Succeeded)
                    logger.LogError(
                        "Failed to assign role {Role} to {Email}: {Errors}",
                        account.Role, account.Email,
                        string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                else
                    logger.LogInformation("Assigned role {Role} to {Email}", account.Role, account.Email);
            }
        }
    }
}
