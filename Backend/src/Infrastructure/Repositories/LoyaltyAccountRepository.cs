using Application.Abstractions;
using Domain.Loyalty;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class LoyaltyAccountRepository(AppDbContext dbContext) : ILoyaltyAccountRepository
{
    public Task<LoyaltyAccount?> GetByIdAsync(int loyaltyAccountId, CancellationToken cancellationToken) =>
        dbContext.LoyaltyAccounts.FirstOrDefaultAsync(a => a.Id == loyaltyAccountId, cancellationToken);

    public Task<LoyaltyAccount?> GetByCustomerAndProgramAsync(int customerId, int loyaltyProgramId, CancellationToken cancellationToken) =>
        dbContext.LoyaltyAccounts.FirstOrDefaultAsync(a => a.CustomerId == customerId && a.LoyaltyProgramId == loyaltyProgramId, cancellationToken);

    public void Add(LoyaltyAccount loyaltyAccount) => dbContext.LoyaltyAccounts.Add(loyaltyAccount);

    public async Task<bool> TryDebitPointsAsync(int loyaltyAccountId, int points, CancellationToken cancellationToken)
    {
        var rowsAffected = await dbContext.LoyaltyAccounts
            .Where(a => a.Id == loyaltyAccountId && a.PointsBalance >= points)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.PointsBalance, a => a.PointsBalance - points), cancellationToken);

        return rowsAffected == 1;
    }
}
