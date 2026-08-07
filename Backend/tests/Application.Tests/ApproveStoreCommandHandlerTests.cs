using Application.Abstractions;
using Application.Stores.Commands.ApproveStore;
using Application.Subscriptions;
using Domain.Stores;
using Domain.Subscriptions;
using Domain.ValueObjects;
using Microsoft.Extensions.Options;
using Moq;

namespace Application.Tests;

public class ApproveStoreCommandHandlerTests
{
    private const string AdminUserId = "admin-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<ISubscriptionPlanRepository> _subscriptionPlanRepository = new();
    private readonly Mock<IStoreSubscriptionRepository> _storeSubscriptionRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public ApproveStoreCommandHandlerTests()
    {
        // No plan configured by default — the handler must approve the store either way (plan
        // issuance is best-effort, see ApproveStoreCommandHandler's own comment on this).
        _subscriptionPlanRepository.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private ApproveStoreCommandHandler CreateHandler() => new(
        _storeRepository.Object, _subscriptionPlanRepository.Object, _storeSubscriptionRepository.Object,
        _auditLogRepository.Object, Options.Create(new SubscriptionOptions()), _unitOfWork.Object);

    private static Store CreateStore(StoreStatus status) => new()
    {
        Id = StoreId,
        OwnerUserId = "owner-1",
        Name = "Test",
        Address = "Addr",
        Location = new GeoLocation(0, 0),
        Status = status
    };

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new ApproveStoreCommand(StoreId, AdminUserId), CancellationToken.None);

        Assert.Equal(ApproveStoreOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_AlreadyApproved_ReturnsAlreadyApproved()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateStore(StoreStatus.Active));

        var handler = CreateHandler();
        var result = await handler.Handle(new ApproveStoreCommand(StoreId, AdminUserId), CancellationToken.None);

        Assert.Equal(ApproveStoreOutcome.AlreadyApproved, result.Outcome);
    }

    [Fact]
    public async Task Handle_PendingStore_ApprovesItAndRecordsAuditLog()
    {
        var store = CreateStore(StoreStatus.PendingApproval);
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(store);

        var handler = CreateHandler();
        var result = await handler.Handle(new ApproveStoreCommand(StoreId, AdminUserId), CancellationToken.None);

        Assert.Equal(ApproveStoreOutcome.Approved, result.Outcome);
        Assert.Equal(StoreStatus.Active, store.Status);
        _auditLogRepository.Verify(r => r.Add(It.Is<Domain.Auditing.AuditLog>(a => a.Action == "Store.Approved" && a.EntityId == StoreId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PendingStoreWithActivePlanAvailable_IssuesTrialSubscription()
    {
        var store = CreateStore(StoreStatus.PendingApproval);
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(store);

        var plan = new SubscriptionPlan { Name = "Basic", Code = "basic", MonthlyPrice = new Money(100, "TJS") };
        plan.Id = 7;
        _subscriptionPlanRepository.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync([plan]);
        _storeSubscriptionRepository.Setup(r => r.GetByStoreIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((StoreSubscription?)null);

        var handler = CreateHandler();
        await handler.Handle(new ApproveStoreCommand(StoreId, AdminUserId), CancellationToken.None);

        _storeSubscriptionRepository.Verify(r => r.Add(It.Is<StoreSubscription>(
            s => s.StoreId == StoreId && s.SubscriptionPlanId == 7 && s.Status == SubscriptionStatus.Trial)), Times.Once);
    }
}
