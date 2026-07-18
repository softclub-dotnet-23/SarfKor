using Application.Abstractions;
using Domain.Offers;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class ExpiringOfferRepository(AppDbContext dbContext) : IExpiringOfferRepository
{
    public void Add(ExpiringOffer offer) => dbContext.ExpiringOffers.Add(offer);
}
