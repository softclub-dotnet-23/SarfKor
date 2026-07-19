using Application.Abstractions;
using Domain.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class UserConsentRepository(AppDbContext dbContext) : IUserConsentRepository
{
    public async Task<IReadOnlyList<UserConsent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.UserConsents.Where(c => c.UserId == userId).ToListAsync(cancellationToken);

    public Task<UserConsent?> GetByUserIdAndTypeAsync(string userId, ConsentType type, CancellationToken cancellationToken) =>
        dbContext.UserConsents.FirstOrDefaultAsync(c => c.UserId == userId && c.Type == type, cancellationToken);

    public void Add(UserConsent userConsent) => dbContext.UserConsents.Add(userConsent);
}
