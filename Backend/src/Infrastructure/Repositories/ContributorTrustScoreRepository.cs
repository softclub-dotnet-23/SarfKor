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

    public async Task<IReadOnlyList<ContributorTrustScore>> GetAllAsync(int skip, int take, CancellationToken cancellationToken) =>
        await dbContext.ContributorTrustScores.OrderBy(c => c.Score).Skip(skip).Take(take).ToListAsync(cancellationToken);

    public Task<int> CountAllAsync(CancellationToken cancellationToken) =>
        dbContext.ContributorTrustScores.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<ContributorTrustScore>> GetAllForDecayAsync(CancellationToken cancellationToken) =>
        await dbContext.ContributorTrustScores.ToListAsync(cancellationToken);
}
