using Domain.Payments;

namespace Application.Abstractions;

public interface IGiftCardRepository
{
    Task<GiftCard?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    void Add(GiftCard giftCard);
}
