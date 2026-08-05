using Application.Abstractions;
using Domain.Reputation;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ContributorTrustScoreAdjustmentRepository(AppDbContext dbContext) : IContributorTrustScoreAdjustmentRepository
{
    public void Add(ContributorTrustScoreAdjustment adjustment) => dbContext.ContributorTrustScoreAdjustments.Add(adjustment);

    public async Task<IReadOnlyList<ContributorTrustScoreAdjustment>> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.ContributorTrustScoreAdjustments
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync(cancellationToken);
}
