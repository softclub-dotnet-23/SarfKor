using Application.Abstractions;
using Application.Stores.Commands.SetStoreEmployeeActive;
using Domain.Auditing;
using Domain.Stores;
using Moq;

namespace Application.Tests;

// Code review 2026-08-10: this handler previously had no test coverage at all despite being where
// three of that review's findings were fixed -- the subscription gate (shared with every other
// StorePartner write handler), the self-disable guard (finding #7), and the refresh-token
// revocation on disable (finding #6). All three get their own case here.
public class SetStoreEmployeeActiveCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const string CashierUserId = "cashier-1";
    private const int StoreId = 1;
    private const int StoreEmployeeId = 5;

    private readonly Mock<IStoreEmployeeRepository> _storeEmployeeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private SetStoreEmployeeActiveCommandHandler CreateHandler() => new(
        _storeEmployeeRepository.Object, _storeAccessAuthorizer.Object, _refreshTokenRepository.Object, _auditLogRepository.Object, _unitOfWork.Object);

    private static StoreEmployee CreateEmployee(string userId = CashierUserId) => new()
    {
        Id = StoreEmployeeId,
        StoreId = StoreId,
        UserId = userId,
        Role = StoreEmployeeRole.Cashier,
        AddedAt = DateTimeOffset.UtcNow,
        IsActive = true
    };

    private static SetStoreEmployeeActiveCommand DisableCommand() => new(StoreEmployeeId, false, OwnerId);

    [Fact]
    public async Task Handle_EmployeeNotFound_ReturnsNotFound()
    {
        _storeEmployeeRepository.Setup(r => r.GetByIdAsync(StoreEmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync((StoreEmployee?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(DisableCommand(), CancellationToken.None);

        Assert.Equal(SetStoreEmployeeActiveOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeEmployeeRepository.Setup(r => r.GetByIdAsync(StoreEmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateEmployee());
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, "someone-else", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(DisableCommand() with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(SetStoreEmployeeActiveOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_OwnerTargetsOwnRow_ReturnsCannotDisableSelf()
    {
        // The owner's own StoreEmployee row -- UserId matches PerformedByUserId.
        _storeEmployeeRepository.Setup(r => r.GetByIdAsync(StoreEmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateEmployee(OwnerId));
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(DisableCommand(), CancellationToken.None);

        Assert.Equal(SetStoreEmployeeActiveOutcome.CannotDisableSelf, result.Outcome);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SubscriptionNotOperational_ReturnsSubscriptionInactive()
    {
        _storeEmployeeRepository.Setup(r => r.GetByIdAsync(StoreEmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateEmployee());
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(DisableCommand(), CancellationToken.None);

        Assert.Equal(SetStoreEmployeeActiveOutcome.SubscriptionInactive, result.Outcome);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Disable_SetsInactiveAndRevokesRefreshTokensAndAudits()
    {
        var employee = CreateEmployee();
        _storeEmployeeRepository.Setup(r => r.GetByIdAsync(StoreEmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(DisableCommand(), CancellationToken.None);

        Assert.Equal(SetStoreEmployeeActiveOutcome.Updated, result.Outcome);
        Assert.False(employee.IsActive);
        _storeEmployeeRepository.Verify(r => r.Update(employee), Times.Once);
        _refreshTokenRepository.Verify(r => r.RevokeAllForUserAsync(CashierUserId, It.IsAny<CancellationToken>()), Times.Once);
        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(a => a.Action == "CashierAccount.Disabled")), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Enable_DoesNotRevokeRefreshTokens()
    {
        var employee = CreateEmployee();
        employee.IsActive = false;
        _storeEmployeeRepository.Setup(r => r.GetByIdAsync(StoreEmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(DisableCommand() with { IsActive = true }, CancellationToken.None);

        Assert.Equal(SetStoreEmployeeActiveOutcome.Updated, result.Outcome);
        Assert.True(employee.IsActive);
        _refreshTokenRepository.Verify(r => r.RevokeAllForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(a => a.Action == "CashierAccount.Enabled")), Times.Once);
    }
}
