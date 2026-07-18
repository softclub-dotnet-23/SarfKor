using Domain.Loyalty;

namespace Application.Abstractions;

public interface ILoyaltyProgramRepository
{
    Task<LoyaltyProgram?> GetByIdAsync(int loyaltyProgramId, CancellationToken cancellationToken);
    Task<LoyaltyProgram?> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken);
    void Add(LoyaltyProgram loyaltyProgram);
}
