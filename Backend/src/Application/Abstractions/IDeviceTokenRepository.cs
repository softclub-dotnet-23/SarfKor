using Domain.Notifications;

namespace Application.Abstractions;

public interface IDeviceTokenRepository
{
    Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);
    void Add(DeviceToken deviceToken);
}
