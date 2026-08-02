using Domain.Loyalty;

namespace Application.Abstractions;

public interface ILoyaltyAccountRepository
{
    Task<LoyaltyAccount?> GetByIdAsync(int loyaltyAccountId, CancellationToken cancellationToken);
    Task<LoyaltyAccount?> GetByCustomerAndProgramAsync(int customerId, int loyaltyProgramId, CancellationToken cancellationToken);
    void Add(LoyaltyAccount loyaltyAccount);

    /// <summary>Atomically debits the points balance only if enough remains. Returns false if insufficient (no write happens).</summary>
    Task<bool> TryDebitPointsAsync(int loyaltyAccountId, int points, CancellationToken cancellationToken);
}
