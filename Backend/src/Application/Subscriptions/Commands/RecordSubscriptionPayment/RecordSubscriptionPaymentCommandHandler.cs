using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Subscriptions;
using Domain.ValueObjects;

namespace Application.Subscriptions.Commands.RecordSubscriptionPayment;

public sealed class RecordSubscriptionPaymentCommandHandler(
    IStoreSubscriptionRepository storeSubscriptionRepository,
    ISubscriptionPaymentRepository subscriptionPaymentRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RecordSubscriptionPaymentCommand, RecordSubscriptionPaymentResult>
{
    public async Task<RecordSubscriptionPaymentResult> Handle(RecordSubscriptionPaymentCommand command, CancellationToken cancellationToken)
    {
        var subscription = await storeSubscriptionRepository.GetByIdAsync(command.StoreSubscriptionId, cancellationToken);
        if (subscription is null)
            return new RecordSubscriptionPaymentResult(RecordSubscriptionPaymentOutcome.SubscriptionNotFound, null, null);

        var payment = new SubscriptionPayment
        {
            StoreSubscriptionId = subscription.Id,
            Amount = new Money(command.Amount, command.Currency),
            PeriodStart = command.PeriodStart,
            PeriodEnd = command.PeriodEnd,
            Method = command.Method,
            Comment = command.Comment,
            Source = SubscriptionPaymentSource.ManualAdmin,
            RecordedByUserId = command.PerformedByUserId,
            RecordedAt = DateTimeOffset.UtcNow
        };
        subscriptionPaymentRepository.Add(payment);

        // ADMIN_PROMPT.md §2.1: "внесение платежа продлевает период и переводит подписку в Active" —
        // unconditional, not just when the paid-through period covers the current gap, so a payment
        // recorded for a future period (e.g. paying next month early) also clears a PastDue/Suspended
        // state immediately rather than waiting for the nightly job to notice.
        var periodEndUtc = command.PeriodEnd.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        if (periodEndUtc > subscription.CurrentPeriodEndsAt.UtcDateTime)
            subscription.CurrentPeriodEndsAt = periodEndUtc;
        subscription.Status = SubscriptionStatus.Active;

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "SubscriptionPayment.Recorded",
            EntityType = nameof(SubscriptionPayment),
            EntityId = payment.Id,
            Details = $"{command.Amount} {command.Currency} for store subscription #{subscription.Id}, period {command.PeriodStart}–{command.PeriodEnd}",
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RecordSubscriptionPaymentResult(RecordSubscriptionPaymentOutcome.Recorded, payment.Id, subscription.CurrentPeriodEndsAt);
    }
}
