using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Subscriptions;

namespace Application.Subscriptions.Commands.ReverseSubscriptionPayment;

// Payments are immutable (ADMIN_PROMPT.md §2.1: "исправление делается сторнирующей записью, а не
// редактированием") — this never updates or deletes the original row, only appends a negative
// counter-entry linked back to it. Deliberately does NOT also roll back the StoreSubscription's
// CurrentPeriodEndsAt/Status that the original payment advanced — reconstructing "what it would
// have been" is exactly the kind of implicit, hard-to-audit state mutation the append-only design
// is meant to avoid; an Admin who needs the subscription itself corrected makes that a separate,
// explicit action (ChangeStoreSubscriptionPlanCommand / another RecordSubscriptionPaymentCommand).
public sealed class ReverseSubscriptionPaymentCommandHandler(
    ISubscriptionPaymentRepository subscriptionPaymentRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ReverseSubscriptionPaymentCommand, ReverseSubscriptionPaymentResult>
{
    public async Task<ReverseSubscriptionPaymentResult> Handle(ReverseSubscriptionPaymentCommand command, CancellationToken cancellationToken)
    {
        var original = await subscriptionPaymentRepository.GetByIdAsync(command.SubscriptionPaymentId, cancellationToken);
        if (original is null)
            return new ReverseSubscriptionPaymentResult(ReverseSubscriptionPaymentOutcome.NotFound, null);

        var siblings = await subscriptionPaymentRepository.GetByStoreSubscriptionIdAsync(original.StoreSubscriptionId, cancellationToken);
        if (siblings.Any(p => p.ReversedPaymentId == original.Id))
            return new ReverseSubscriptionPaymentResult(ReverseSubscriptionPaymentOutcome.AlreadyReversed, null);

        // Money can't hold a negative amount by design (see Domain.ValueObjects.Money) — the
        // reversal keeps the original's positive magnitude and lets IsReversal=true tell readers to
        // subtract it, rather than encoding the sign in the amount itself.
        var reversal = new SubscriptionPayment
        {
            StoreSubscriptionId = original.StoreSubscriptionId,
            Amount = original.Amount,
            PeriodStart = original.PeriodStart,
            PeriodEnd = original.PeriodEnd,
            Method = original.Method,
            Comment = command.Reason,
            Source = SubscriptionPaymentSource.ManualAdmin,
            RecordedByUserId = command.PerformedByUserId,
            RecordedAt = DateTimeOffset.UtcNow,
            IsReversal = true,
            ReversedPaymentId = original.Id
        };
        subscriptionPaymentRepository.Add(reversal);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "SubscriptionPayment.Reversed",
            EntityType = nameof(SubscriptionPayment),
            EntityId = original.Id,
            Reason = command.Reason,
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReverseSubscriptionPaymentResult(ReverseSubscriptionPaymentOutcome.Reversed, reversal.Id);
    }
}
