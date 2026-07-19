using Application.Abstractions;
using Domain.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class UserProfileRepository(AppDbContext dbContext) : IUserProfileRepository
{
    public Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public void Add(UserProfile userProfile) => dbContext.UserProfiles.Add(userProfile);
}
