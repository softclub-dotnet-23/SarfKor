using Application.Abstractions;
using Domain.Reputation;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ContributorTrustScoreRepository(AppDbContext dbContext) : IContributorTrustScoreRepository
{
    public Task<ContributorTrustScore?> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        dbContext.ContributorTrustScores.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public void Add(ContributorTrustScore trustScore) => dbContext.ContributorTrustScores.Add(trustScore);
}
