using Domain.Loyalty;

namespace Application.Abstractions;

public interface ILoyaltyAccountRepository
{
    Task<LoyaltyAccount?> GetByIdAsync(int loyaltyAccountId, CancellationToken cancellationToken);
    Task<LoyaltyAccount?> GetByCustomerAndProgramAsync(int customerId, int loyaltyProgramId, CancellationToken cancellationToken);
    void Add(LoyaltyAccount loyaltyAccount);
}
