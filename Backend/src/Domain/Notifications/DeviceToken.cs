using Domain.Common;

namespace Domain.Notifications;

public class DeviceToken : Entity
{
    public required string UserId { get; set; }
    public required string Token { get; set; }
    public DevicePlatform Platform { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
}
