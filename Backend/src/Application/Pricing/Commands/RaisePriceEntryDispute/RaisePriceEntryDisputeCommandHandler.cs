using Application.Abstractions;
using Application.Common;
using Domain.Pricing;

namespace Application.Pricing.Commands.RaisePriceEntryDispute;

public sealed class RaisePriceEntryDisputeCommandHandler(
    IPriceEntryRepository priceEntryRepository,
    IPriceEntryDisputeRepository priceEntryDisputeRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RaisePriceEntryDisputeCommand, RaisePriceEntryDisputeResult>
{
    public async Task<RaisePriceEntryDisputeResult> Handle(RaisePriceEntryDisputeCommand command, CancellationToken cancellationToken)
    {
        if (await priceEntryRepository.GetByIdAsync(command.PriceEntryId, cancellationToken) is null)
            return new RaisePriceEntryDisputeResult(RaisePriceEntryDisputeOutcome.PriceEntryNotFound, null);

        if (await priceEntryDisputeRepository.HasPendingDisputeAsync(command.PriceEntryId, cancellationToken))
            return new RaisePriceEntryDisputeResult(RaisePriceEntryDisputeOutcome.AlreadyDisputed, null);

        var dispute = new PriceEntryDispute
        {
            PriceEntryId = command.PriceEntryId,
            DisputedByUserId = command.DisputedByUserId,
            Reason = command.Reason,
            Status = PriceEntryDisputeStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        priceEntryDisputeRepository.Add(dispute);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RaisePriceEntryDisputeResult(RaisePriceEntryDisputeOutcome.Raised, dispute.Id);
    }
}
