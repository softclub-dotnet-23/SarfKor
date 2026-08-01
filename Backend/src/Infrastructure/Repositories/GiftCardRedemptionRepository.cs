using Application.Abstractions;
using Domain.Payments;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class GiftCardRedemptionRepository(AppDbContext dbContext) : IGiftCardRedemptionRepository
{
    public void Add(GiftCardRedemption redemption) => dbContext.GiftCardRedemptions.Add(redemption);
}
