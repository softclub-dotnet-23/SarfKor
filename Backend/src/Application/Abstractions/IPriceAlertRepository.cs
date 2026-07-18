using Domain.Notifications;

namespace Application.Abstractions;

public interface IPriceAlertRepository
{
    Task<PriceAlert?> GetByIdAsync(int priceAlertId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceAlert>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    void Add(PriceAlert priceAlert);
}
