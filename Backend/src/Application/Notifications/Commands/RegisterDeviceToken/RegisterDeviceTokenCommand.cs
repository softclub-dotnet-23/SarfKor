using Domain.Notifications;

namespace Application.Notifications.Commands.RegisterDeviceToken;

public sealed record RegisterDeviceTokenCommand(string UserId, string Token, DevicePlatform Platform);
