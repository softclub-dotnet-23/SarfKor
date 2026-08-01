using Application.Abstractions;
using Application.Stores.Commands.ApproveStore;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class ApproveStoreCommandHandlerTests
{
    private const string AdminUserId = "admin-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ApproveStoreCommandHandler CreateHandler() => new(_storeRepository.Object, _auditLogRepository.Object, _unitOfWork.Object);

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
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateStore(StoreStatus.Approved));

        var handler = CreateHandler();
        var result = await handler.Handle(new ApproveStoreCommand(StoreId, AdminUserId), CancellationToken.None);

        Assert.Equal(ApproveStoreOutcome.AlreadyApproved, result.Outcome);
    }

    [Fact]
    public async Task Handle_PendingStore_ApprovesItAndRecordsAuditLog()
    {
        var store = CreateStore(StoreStatus.Pending);
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(store);

        var handler = CreateHandler();
        var result = await handler.Handle(new ApproveStoreCommand(StoreId, AdminUserId), CancellationToken.None);

        Assert.Equal(ApproveStoreOutcome.Approved, result.Outcome);
        Assert.Equal(StoreStatus.Approved, store.Status);
        _auditLogRepository.Verify(r => r.Add(It.Is<Domain.Auditing.AuditLog>(a => a.Action == "Store.Approved" && a.EntityId == StoreId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
