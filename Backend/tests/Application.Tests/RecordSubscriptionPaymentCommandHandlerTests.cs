using Application.Abstractions;
using Application.Subscriptions.Commands.RecordSubscriptionPayment;
using Domain.Auditing;
using Domain.Subscriptions;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class RecordSubscriptionPaymentCommandHandlerTests
{
    private const string AdminUserId = "admin-1";
    private const int SubscriptionId = 1;

    private readonly Mock<IStoreSubscriptionRepository> _storeSubscriptionRepository = new();
    private readonly Mock<ISubscriptionPaymentRepository> _subscriptionPaymentRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RecordSubscriptionPaymentCommandHandler CreateHandler() => new(
        _storeSubscriptionRepository.Object, _subscriptionPaymentRepository.Object, _auditLogRepository.Object, _unitOfWork.Object);

    private static StoreSubscription CreateSubscription(SubscriptionStatus status, DateTimeOffset currentPeriodEndsAt)
    {
        var subscription = new StoreSubscription
        {
            StoreId = 1,
            SubscriptionPlanId = 1,
            Status = status,
            StartedAt = DateTimeOffset.UtcNow.AddMonths(-1),
            CurrentPeriodEndsAt = currentPeriodEndsAt,
            PriceAtIssue = new Money(100, "TJS")
        };
        subscription.Id = SubscriptionId;
        return subscription;
    }

    [Fact]
    public async Task Handle_SubscriptionNotFound_ReturnsSubscriptionNotFound()
    {
        _storeSubscriptionRepository.Setup(r => r.GetByIdAsync(SubscriptionId, It.IsAny<CancellationToken>())).ReturnsAsync((StoreSubscription?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RecordSubscriptionPaymentCommand(SubscriptionId, 100, "TJS", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), SubscriptionPaymentMethod.Cash, null, AdminUserId),
            CancellationToken.None);

        Assert.Equal(RecordSubscriptionPaymentOutcome.SubscriptionNotFound, result.Outcome);
    }

    // ADMIN_PROMPT.md §2.1: "внесение платежа продлевает период и переводит подписку в Active" —
    // covers exactly the Suspended/PastDue -> Active status transition a payment is meant to trigger.
    [Theory]
    [InlineData(SubscriptionStatus.Suspended)]
    [InlineData(SubscriptionStatus.PastDue)]
    [InlineData(SubscriptionStatus.Trial)]
    public async Task Handle_RecordingPayment_RevivesSubscriptionToActiveAndExtendsPeriod(SubscriptionStatus initialStatus)
    {
        var subscription = CreateSubscription(initialStatus, DateTimeOffset.UtcNow.AddDays(-10));
        _storeSubscriptionRepository.Setup(r => r.GetByIdAsync(SubscriptionId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var periodStart = DateOnly.FromDateTime(DateTime.UtcNow);
        var periodEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RecordSubscriptionPaymentCommand(SubscriptionId, 100, "TJS", periodStart, periodEnd, SubscriptionPaymentMethod.Cash, "monthly fee", AdminUserId),
            CancellationToken.None);

        Assert.Equal(RecordSubscriptionPaymentOutcome.Recorded, result.Outcome);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(periodEnd.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), subscription.CurrentPeriodEndsAt.UtcDateTime);
        _subscriptionPaymentRepository.Verify(r => r.Add(It.Is<SubscriptionPayment>(
            p => p.StoreSubscriptionId == SubscriptionId && p.Amount.Amount == 100 && p.RecordedByUserId == AdminUserId)), Times.Once);
        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(a => a.Action == "SubscriptionPayment.Recorded")), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // A payment for a period that ends before the currently-recorded period end (e.g. a backdated
    // correction) must not shorten what's already paid for.
    [Fact]
    public async Task Handle_PeriodEndEarlierThanCurrent_DoesNotShortenCurrentPeriod()
    {
        var farFuture = DateTimeOffset.UtcNow.AddMonths(2);
        var subscription = CreateSubscription(SubscriptionStatus.Active, farFuture);
        _storeSubscriptionRepository.Setup(r => r.GetByIdAsync(SubscriptionId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var handler = CreateHandler();
        await handler.Handle(
            new RecordSubscriptionPaymentCommand(
                SubscriptionId, 100, "TJS", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                SubscriptionPaymentMethod.Cash, null, AdminUserId),
            CancellationToken.None);

        Assert.Equal(farFuture.UtcDateTime, subscription.CurrentPeriodEndsAt.UtcDateTime);
    }
}
