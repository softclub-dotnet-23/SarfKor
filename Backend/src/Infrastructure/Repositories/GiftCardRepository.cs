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
}
