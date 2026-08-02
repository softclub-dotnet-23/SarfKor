using Domain.Payments;

namespace Application.Abstractions;

public interface IGiftCardRedemptionRepository
{
    void Add(GiftCardRedemption redemption);
}
