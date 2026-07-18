using Application.Abstractions;
using Application.Common;

namespace Application.Notifications.Commands.DeactivatePriceAlert;

public sealed class DeactivatePriceAlertCommandHandler(
    IPriceAlertRepository priceAlertRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeactivatePriceAlertCommand, DeactivatePriceAlertResult>
{
    public async Task<DeactivatePriceAlertResult> Handle(DeactivatePriceAlertCommand command, CancellationToken cancellationToken)
    {
        var alert = await priceAlertRepository.GetByIdAsync(command.PriceAlertId, cancellationToken);
        if (alert is null)
            return new DeactivatePriceAlertResult(DeactivatePriceAlertOutcome.NotFound);

        if (alert.UserId != command.UserId)
            return new DeactivatePriceAlertResult(DeactivatePriceAlertOutcome.Forbidden);

        alert.IsActive = false;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeactivatePriceAlertResult(DeactivatePriceAlertOutcome.Deactivated);
    }
}
