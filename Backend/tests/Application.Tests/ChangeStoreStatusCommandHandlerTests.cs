using Application.Abstractions;
using Application.Stores.Commands.ChangeStoreStatus;
using Domain.Auditing;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class ChangeStoreStatusCommandHandlerTests
{
    private const string AdminUserId = "admin-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ChangeStoreStatusCommandHandler CreateHandler() => new(_storeRepository.Object, _auditLogRepository.Object, _unitOfWork.Object);

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
        var result = await handler.Handle(new ChangeStoreStatusCommand(StoreId, StoreStatus.Suspended, "reason", AdminUserId), CancellationToken.None);

        Assert.Equal(ChangeStoreStatusOutcome.NotFound, result.Outcome);
    }

    // The legal-transition matrix (ADMIN_PROMPT.md §2.2): PendingApproval->[Rejected],
    // Active->[Suspended,Blocked,Archived], Suspended->[Active,Blocked,Archived], Blocked->[Active,Archived].
    // Approve (PendingApproval->Active) is deliberately excluded — that's ApproveStoreCommand.
    [Theory]
    [InlineData(StoreStatus.PendingApproval, StoreStatus.Rejected)]
    [InlineData(StoreStatus.Active, StoreStatus.Suspended)]
    [InlineData(StoreStatus.Active, StoreStatus.Blocked)]
    [InlineData(StoreStatus.Active, StoreStatus.Archived)]
    [InlineData(StoreStatus.Suspended, StoreStatus.Active)]
    [InlineData(StoreStatus.Suspended, StoreStatus.Blocked)]
    [InlineData(StoreStatus.Suspended, StoreStatus.Archived)]
    [InlineData(StoreStatus.Blocked, StoreStatus.Active)]
    [InlineData(StoreStatus.Blocked, StoreStatus.Archived)]
    public async Task Handle_LegalTransition_ChangesStatusAndRecordsAuditLog(StoreStatus from, StoreStatus to)
    {
        var store = CreateStore(from);
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(store);

        var handler = CreateHandler();
        var result = await handler.Handle(new ChangeStoreStatusCommand(StoreId, to, "a documented reason", AdminUserId), CancellationToken.None);

        Assert.Equal(ChangeStoreStatusOutcome.Changed, result.Outcome);
        Assert.Equal(to, store.Status);
        Assert.Equal("a documented reason", store.StatusReason);
        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(
            a => a.Action == $"Store.{to}" && a.EntityId == StoreId && a.Reason == "a documented reason" && a.PerformedByUserId == AdminUserId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Terminal states (Archived/Rejected) and the reverse-of-legal direction (e.g. Active->PendingApproval)
    // must never be reachable through this handler.
    [Theory]
    [InlineData(StoreStatus.Archived, StoreStatus.Active)]
    [InlineData(StoreStatus.Rejected, StoreStatus.Active)]
    [InlineData(StoreStatus.PendingApproval, StoreStatus.Active)]
    [InlineData(StoreStatus.PendingApproval, StoreStatus.Suspended)]
    [InlineData(StoreStatus.Active, StoreStatus.PendingApproval)]
    public async Task Handle_IllegalTransition_ReturnsIllegalTransitionAndDoesNotMutate(StoreStatus from, StoreStatus to)
    {
        var store = CreateStore(from);
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(store);

        var handler = CreateHandler();
        var result = await handler.Handle(new ChangeStoreStatusCommand(StoreId, to, "reason", AdminUserId), CancellationToken.None);

        Assert.Equal(ChangeStoreStatusOutcome.IllegalTransition, result.Outcome);
        Assert.Equal(from, store.Status);
        _auditLogRepository.Verify(r => r.Add(It.IsAny<AuditLog>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordsPerformedByIpAddressWhenProvided()
    {
        var store = CreateStore(StoreStatus.Active);
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(store);

        var handler = CreateHandler();
        await handler.Handle(new ChangeStoreStatusCommand(StoreId, StoreStatus.Suspended, "reason", AdminUserId, "203.0.113.7"), CancellationToken.None);

        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(a => a.IpAddress == "203.0.113.7")), Times.Once);
    }
}
