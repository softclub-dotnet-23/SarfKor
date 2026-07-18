using Domain.Offers;

namespace Application.Abstractions;

public interface IExpiringOfferRepository
{
    void Add(ExpiringOffer offer);
}
