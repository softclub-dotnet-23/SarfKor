using Application.Abstractions;
using Application.Common;
using Domain.Notifications;

namespace Application.Notifications.Commands.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommandHandler(
    IDeviceTokenRepository deviceTokenRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RegisterDeviceTokenCommand, RegisterDeviceTokenResult>
{
    public async Task<RegisterDeviceTokenResult> Handle(RegisterDeviceTokenCommand command, CancellationToken cancellationToken)
    {
        // Idempotent upsert: the same physical device token re-registering (app reinstall, user
        // switch on a shared device) updates the existing row instead of accumulating duplicates.
        var existing = await deviceTokenRepository.GetByTokenAsync(command.Token, cancellationToken);
        if (existing is not null)
        {
            existing.UserId = command.UserId;
            existing.Platform = command.Platform;
            existing.RegisteredAt = DateTimeOffset.UtcNow;

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new RegisterDeviceTokenResult(existing.Id);
        }

        var deviceToken = new DeviceToken
        {
            UserId = command.UserId,
            Token = command.Token,
            Platform = command.Platform,
            RegisteredAt = DateTimeOffset.UtcNow
        };

        deviceTokenRepository.Add(deviceToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterDeviceTokenResult(deviceToken.Id);
    }
}
