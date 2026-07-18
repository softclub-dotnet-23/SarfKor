using Application.Abstractions;
using Domain.Loyalty;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class LoyaltyTransactionRepository(AppDbContext dbContext) : ILoyaltyTransactionRepository
{
    public void Add(LoyaltyTransaction loyaltyTransaction) => dbContext.LoyaltyTransactions.Add(loyaltyTransaction);
}
