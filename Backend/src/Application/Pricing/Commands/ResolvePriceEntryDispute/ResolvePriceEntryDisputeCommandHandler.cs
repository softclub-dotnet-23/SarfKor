using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Pricing;

namespace Application.Pricing.Commands.ResolvePriceEntryDispute;

public sealed class ResolvePriceEntryDisputeCommandHandler(
    IPriceEntryDisputeRepository priceEntryDisputeRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ResolvePriceEntryDisputeCommand, ResolvePriceEntryDisputeResult>
{
    public async Task<ResolvePriceEntryDisputeResult> Handle(ResolvePriceEntryDisputeCommand command, CancellationToken cancellationToken)
    {
        var dispute = await priceEntryDisputeRepository.GetByIdAsync(command.DisputeId, cancellationToken);
        if (dispute is null)
            return new ResolvePriceEntryDisputeResult(ResolvePriceEntryDisputeOutcome.NotFound);

        if (dispute.Status != PriceEntryDisputeStatus.Pending)
            return new ResolvePriceEntryDisputeResult(ResolvePriceEntryDisputeOutcome.AlreadyResolved);

        dispute.Status = command.Uphold ? PriceEntryDisputeStatus.Upheld : PriceEntryDisputeStatus.Dismissed;

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.AdminUserId,
            Action = command.Uphold ? "PriceEntryDispute.Upheld" : "PriceEntryDispute.Dismissed",
            EntityType = nameof(PriceEntryDispute),
            EntityId = dispute.Id,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ResolvePriceEntryDisputeResult(command.Uphold ? ResolvePriceEntryDisputeOutcome.Upheld : ResolvePriceEntryDisputeOutcome.Dismissed);
    }
}
