using Domain.Loyalty;

namespace Application.Abstractions;

public interface ILoyaltyTransactionRepository
{
    void Add(LoyaltyTransaction loyaltyTransaction);
}
