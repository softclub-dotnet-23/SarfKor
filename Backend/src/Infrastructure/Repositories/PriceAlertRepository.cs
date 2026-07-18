using Application.Abstractions;
using Domain.Notifications;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PriceAlertRepository(AppDbContext dbContext) : IPriceAlertRepository
{
    public Task<PriceAlert?> GetByIdAsync(int priceAlertId, CancellationToken cancellationToken) =>
        dbContext.PriceAlerts.FirstOrDefaultAsync(a => a.Id == priceAlertId, cancellationToken);

    public async Task<IReadOnlyList<PriceAlert>> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.PriceAlerts.Where(a => a.UserId == userId).ToListAsync(cancellationToken);

    public void Add(PriceAlert priceAlert) => dbContext.PriceAlerts.Add(priceAlert);
}
