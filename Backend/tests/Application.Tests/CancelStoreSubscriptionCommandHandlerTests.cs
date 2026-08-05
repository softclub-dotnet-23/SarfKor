using Application.Abstractions;
using Application.Subscriptions.Commands.CancelStoreSubscription;
using Domain.Auditing;
using Domain.Subscriptions;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class CancelStoreSubscriptionCommandHandlerTests
{
    private const string AdminUserId = "admin-1";
    private const int SubscriptionId = 1;

    private readonly Mock<IStoreSubscriptionRepository> _storeSubscriptionRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CancelStoreSubscriptionCommandHandler CreateHandler() =>
        new(_storeSubscriptionRepository.Object, _auditLogRepository.Object, _unitOfWork.Object);

    private static StoreSubscription CreateSubscription(SubscriptionStatus status)
    {
        var subscription = new StoreSubscription
        {
            StoreId = 1,
            SubscriptionPlanId = 1,
            Status = status,
            StartedAt = DateTimeOffset.UtcNow.AddMonths(-1),
            CurrentPeriodEndsAt = DateTimeOffset.UtcNow.AddDays(10),
            PriceAtIssue = new Money(100, "TJS")
        };
        subscription.Id = SubscriptionId;
        return subscription;
    }

    [Fact]
    public async Task Handle_SubscriptionNotFound_ReturnsNotFound()
    {
        _storeSubscriptionRepository.Setup(r => r.GetByIdAsync(SubscriptionId, It.IsAny<CancellationToken>())).ReturnsAsync((StoreSubscription?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new CancelStoreSubscriptionCommand(SubscriptionId, "no longer needed", AdminUserId), CancellationToken.None);

        Assert.Equal(CancelStoreSubscriptionOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_AlreadyCancelled_ReturnsAlreadyCancelledAndDoesNotWriteAuditLog()
    {
        var subscription = CreateSubscription(SubscriptionStatus.Cancelled);
        _storeSubscriptionRepository.Setup(r => r.GetByIdAsync(SubscriptionId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var handler = CreateHandler();
        var result = await handler.Handle(new CancelStoreSubscriptionCommand(SubscriptionId, "no longer needed", AdminUserId), CancellationToken.None);

        Assert.Equal(CancelStoreSubscriptionOutcome.AlreadyCancelled, result.Outcome);
        _auditLogRepository.Verify(r => r.Add(It.IsAny<AuditLog>()), Times.Never);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Trial)]
    [InlineData(SubscriptionStatus.Active)]
    [InlineData(SubscriptionStatus.PastDue)]
    [InlineData(SubscriptionStatus.Suspended)]
    public async Task Handle_AnyNonCancelledStatus_TransitionsToCancelledAndRecordsReason(SubscriptionStatus initialStatus)
    {
        var subscription = CreateSubscription(initialStatus);
        _storeSubscriptionRepository.Setup(r => r.GetByIdAsync(SubscriptionId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var handler = CreateHandler();
        var result = await handler.Handle(new CancelStoreSubscriptionCommand(SubscriptionId, "store closed permanently", AdminUserId), CancellationToken.None);

        Assert.Equal(CancelStoreSubscriptionOutcome.Cancelled, result.Outcome);
        Assert.Equal(SubscriptionStatus.Cancelled, subscription.Status);
        Assert.Equal("store closed permanently", subscription.Note);
        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(
            a => a.Action == "StoreSubscription.Cancelled" && a.Reason == "store closed permanently" && a.EntityId == SubscriptionId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
