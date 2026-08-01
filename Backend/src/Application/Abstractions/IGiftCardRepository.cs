using Domain.Payments;

namespace Application.Abstractions;

public interface IGiftCardRepository
{
    Task<GiftCard?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    void Add(GiftCard giftCard);

    /// <summary>Atomically debits the balance only if enough remains. Returns false if insufficient (no write happens).</summary>
    Task<bool> TryDebitAsync(int giftCardId, decimal amount, CancellationToken cancellationToken);

    /// <summary>Atomically credits (refunds) the balance back onto the card.</summary>
    Task CreditAsync(int giftCardId, decimal amount, CancellationToken cancellationToken);
}
