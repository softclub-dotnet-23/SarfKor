using Application.Abstractions;
using Application.Stores.Commands.AddStoreEmployee;
using Domain.Stores;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests;

public class AddStoreEmployeeCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const string CashierUserId = "cashier-1";
    private const string CashierEmail = "cashier@sarfkor.tj";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreEmployeeRepository> _storeEmployeeRepository = new();
    private readonly Mock<IStoreEmployeeInvitationRepository> _invitationRepository = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<AddStoreEmployeeCommandHandler>> _logger = new();

    private AddStoreEmployeeCommandHandler CreateHandler() =>
        new(_storeRepository.Object, _storeEmployeeRepository.Object, _invitationRepository.Object, _authService.Object, _emailSender.Object, _unitOfWork.Object, _logger.Object);

    private static AddStoreEmployeeCommand ValidCommand() => new(StoreId, CashierEmail, StoreEmployeeRole.Cashier, OwnerId);

    private void SetupOwnedStore() =>
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AddStoreEmployeeOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        SetupOwnedStore();

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand() with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(AddStoreEmployeeOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_EmailNotRegistered_CreatesInvitationAndReturnsInvited()
    {
        SetupOwnedStore();
        _authService.Setup(a => a.FindUserIdByEmailAsync(CashierEmail, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _invitationRepository
            .Setup(r => r.GetPendingByStoreAndEmailAsync(StoreId, CashierEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreEmployeeInvitation?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AddStoreEmployeeOutcome.Invited, result.Outcome);
        _storeEmployeeRepository.Verify(r => r.Add(It.IsAny<StoreEmployee>()), Times.Never);
        _invitationRepository.Verify(r => r.Add(It.Is<StoreEmployeeInvitation>(i => i.StoreId == StoreId && i.Email == CashierEmail)), Times.Once);
        _emailSender.Verify(e => e.SendStoreEmployeeInviteEmailAsync(CashierEmail, "Test", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailAlreadyHasPendingInvitation_ResendsWithoutCreatingDuplicate()
    {
        SetupOwnedStore();
        _authService.Setup(a => a.FindUserIdByEmailAsync(CashierEmail, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var existingInvitation = new StoreEmployeeInvitation
        {
            StoreId = StoreId,
            Email = CashierEmail,
            Role = StoreEmployeeRole.Cashier,
            Token = "existing-token",
            InvitedByUserId = OwnerId,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(23),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _invitationRepository
            .Setup(r => r.GetPendingByStoreAndEmailAsync(StoreId, CashierEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingInvitation);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AddStoreEmployeeOutcome.Invited, result.Outcome);
        _invitationRepository.Verify(r => r.Add(It.IsAny<StoreEmployeeInvitation>()), Times.Never);
        _emailSender.Verify(e => e.SendStoreEmployeeInviteEmailAsync(CashierEmail, "Test", "existing-token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserAlreadyEmployedAtStore_ReturnsAlreadyEmployed()
    {
        SetupOwnedStore();
        _authService.Setup(a => a.FindUserIdByEmailAsync(CashierEmail, It.IsAny<CancellationToken>())).ReturnsAsync(CashierUserId);
        _storeEmployeeRepository
            .Setup(r => r.GetByStoreIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new StoreEmployee { StoreId = StoreId, UserId = CashierUserId, Role = StoreEmployeeRole.Cashier, AddedAt = DateTimeOffset.UtcNow }]);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AddStoreEmployeeOutcome.AlreadyEmployed, result.Outcome);
        _storeEmployeeRepository.Verify(r => r.Add(It.IsAny<StoreEmployee>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_ResolvesEmailAndAddsEmployee()
    {
        SetupOwnedStore();
        _authService.Setup(a => a.FindUserIdByEmailAsync(CashierEmail, It.IsAny<CancellationToken>())).ReturnsAsync(CashierUserId);
        _storeEmployeeRepository.Setup(r => r.GetByStoreIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _storeEmployeeRepository.Setup(r => r.Add(It.IsAny<StoreEmployee>())).Callback<StoreEmployee>(e => e.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AddStoreEmployeeOutcome.Added, result.Outcome);
        Assert.Equal(1, result.StoreEmployeeId);
        _storeEmployeeRepository.Verify(r => r.Add(It.Is<StoreEmployee>(e => e.UserId == CashierUserId)), Times.Once);
        // [Authorize("StorePartner")] gates every POS controller by JWT role claim, checked before
        // the use-case's own "owner or employee of this store" logic ever runs — without this the
        // new cashier would bounce off the attribute and never reach that check at all.
        _authService.Verify(a => a.AssignRoleAsync(CashierUserId, "StorePartner", It.IsAny<CancellationToken>()), Times.Once);
    }
}
