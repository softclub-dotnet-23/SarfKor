using Application.Abstractions;
using Domain.Payments;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class GiftCardRepository(AppDbContext dbContext) : IGiftCardRepository
{
    public Task<GiftCard?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.GiftCards.FirstOrDefaultAsync(g => g.Code == code, cancellationToken);

    public void Add(GiftCard giftCard) => dbContext.GiftCards.Add(giftCard);

    public async Task<bool> TryDebitAsync(int giftCardId, decimal amount, CancellationToken cancellationToken)
    {
        var rowsAffected = await dbContext.GiftCards
            .Where(g => g.Id == giftCardId && g.Balance.Amount >= amount)
            .ExecuteUpdateAsync(s => s.SetProperty(g => g.Balance.Amount, g => g.Balance.Amount - amount), cancellationToken);

        return rowsAffected == 1;
    }

    public async Task CreditAsync(int giftCardId, decimal amount, CancellationToken cancellationToken)
    {
        await dbContext.GiftCards
            .Where(g => g.Id == giftCardId)
            .ExecuteUpdateAsync(s => s.SetProperty(g => g.Balance.Amount, g => g.Balance.Amount + amount), cancellationToken);
    }
}
