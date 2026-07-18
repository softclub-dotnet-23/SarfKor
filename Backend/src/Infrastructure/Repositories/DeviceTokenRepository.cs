using Application.Abstractions;
using Domain.Notifications;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class DeviceTokenRepository(AppDbContext dbContext) : IDeviceTokenRepository
{
    public Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken) =>
        dbContext.DeviceTokens.FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

    public void Add(DeviceToken deviceToken) => dbContext.DeviceTokens.Add(deviceToken);
}
