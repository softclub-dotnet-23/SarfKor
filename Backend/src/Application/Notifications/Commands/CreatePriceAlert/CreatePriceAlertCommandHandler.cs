using Application.Abstractions;
using Application.Common;
using Domain.Notifications;
using Domain.ValueObjects;

namespace Application.Notifications.Commands.CreatePriceAlert;

public sealed class CreatePriceAlertCommandHandler(
    IPriceAlertRepository priceAlertRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreatePriceAlertCommand, CreatePriceAlertResult>
{
    public async Task<CreatePriceAlertResult> Handle(CreatePriceAlertCommand command, CancellationToken cancellationToken)
    {
        var alert = new PriceAlert
        {
            UserId = command.UserId,
            ProductId = command.ProductId,
            TargetPrice = new Money(command.TargetPrice, command.Currency),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        priceAlertRepository.Add(alert);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreatePriceAlertResult(alert.Id);
    }
}
