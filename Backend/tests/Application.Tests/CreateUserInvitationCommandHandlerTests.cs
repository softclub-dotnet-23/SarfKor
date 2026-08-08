using Application.Abstractions;
using Application.Identity.Commands.CreateUserInvitation;
using Application.Stores;
using Domain.Stores;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Application.Tests;

public class CreateUserInvitationCommandHandlerTests
{
    private const string AdminUserId = "admin-1";
    private const string Email = "invitee@sarfkor.tj";
    private const int StoreId = 7;

    private readonly Mock<IStoreEmployeeInvitationRepository> _invitationRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateUserInvitationCommandHandler CreateHandler() => new(
        _invitationRepository.Object,
        _storeRepository.Object,
        _authService.Object,
        _userProfileRepository.Object,
        _emailSender.Object,
        _auditLogRepository.Object,
        Options.Create(new StoreEmployeeInvitationOptions { ExpiryDays = 7 }),
        _unitOfWork.Object,
        new LoggerFactory().CreateLogger<CreateUserInvitationCommandHandler>());

    private void SetPerformerIsAdmin(bool isAdmin) =>
        _authService
            .Setup(a => a.GetUserDetailAsync(AdminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminUserDetail(AdminUserId, "admin@sarfkor.tj", DateTimeOffset.UtcNow, false, null, null, null, isAdmin ? ["Admin"] : ["User"]));

    private static Store ValidStore() => new()
    {
        OwnerUserId = "some-owner",
        Name = "Тестовый магазин",
        Address = "ул. Тестовая, 1",
        Location = new GeoLocation(38.5, 68.7),
        Status = StoreStatus.Active,
    };

    [Fact]
    public async Task Handle_PerformerNotAdmin_ReturnsForbidden()
    {
        SetPerformerIsAdmin(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateUserInvitationCommand(Email, "User", null, AdminUserId), CancellationToken.None);

        Assert.Equal(CreateUserInvitationOutcome.Forbidden, result.Outcome);
        _invitationRepository.Verify(r => r.Add(It.IsAny<StoreEmployeeInvitation>()), Times.Never);
        _emailSender.Verify(
            e => e.SendInvitationEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<StoreEmployeeRole?>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_StorePartnerRoleWithMissingStore_ReturnsStoreNotFound()
    {
        SetPerformerIsAdmin(true);
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateUserInvitationCommand(Email, "StorePartner", StoreId, AdminUserId), CancellationToken.None);

        Assert.Equal(CreateUserInvitationOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_UserRole_CreatesInvitationWithNoStoreOrEmployeeRole()
    {
        SetPerformerIsAdmin(true);
        _invitationRepository
            .Setup(r => r.GetPendingByEmailAndRoleAsync(Email, "User", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreEmployeeInvitation?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateUserInvitationCommand(Email, "User", null, AdminUserId), CancellationToken.None);

        Assert.Equal(CreateUserInvitationOutcome.Sent, result.Outcome);
        _invitationRepository.Verify(
            r => r.Add(It.Is<StoreEmployeeInvitation>(i => i.Email == Email && i.InvitedRole == "User" && i.StoreId == null && i.Role == null)),
            Times.Once);
        _auditLogRepository.Verify(
            r => r.Add(It.Is<Domain.Auditing.AuditLog>(a => a.Action == "UserInvitation.Created" && a.PerformedByUserId == AdminUserId)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AdminRole_CreatesInvitationWithNoStoreOrEmployeeRole()
    {
        SetPerformerIsAdmin(true);
        _invitationRepository
            .Setup(r => r.GetPendingByEmailAndRoleAsync(Email, "Admin", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreEmployeeInvitation?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateUserInvitationCommand(Email, "Admin", null, AdminUserId), CancellationToken.None);

        Assert.Equal(CreateUserInvitationOutcome.Sent, result.Outcome);
        _invitationRepository.Verify(
            r => r.Add(It.Is<StoreEmployeeInvitation>(i => i.InvitedRole == "Admin" && i.StoreId == null && i.Role == null)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_StorePartnerRole_CreatesInvitationScopedToStoreAsOwner()
    {
        SetPerformerIsAdmin(true);
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(ValidStore());
        _invitationRepository
            .Setup(r => r.GetPendingByEmailAndRoleAsync(Email, "StorePartner", StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreEmployeeInvitation?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateUserInvitationCommand(Email, "StorePartner", StoreId, AdminUserId), CancellationToken.None);

        Assert.Equal(CreateUserInvitationOutcome.Sent, result.Outcome);
        _invitationRepository.Verify(
            r => r.Add(It.Is<StoreEmployeeInvitation>(i => i.InvitedRole == "StorePartner" && i.StoreId == StoreId && i.Role == StoreEmployeeRole.Owner)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PendingInvitationAlreadyExists_RefreshesInsteadOfDuplicating()
    {
        SetPerformerIsAdmin(true);
        var existing = new StoreEmployeeInvitation
        {
            StoreId = null,
            Email = Email,
            Role = null,
            InvitedRole = "User",
            TokenHash = "old-hash",
            InvitedByUserId = AdminUserId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            LastSentAt = DateTimeOffset.UtcNow.AddDays(-2),
            Status = StoreEmployeeInvitationStatus.Pending
        };
        _invitationRepository
            .Setup(r => r.GetPendingByEmailAndRoleAsync(Email, "User", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateUserInvitationCommand(Email, "User", null, AdminUserId), CancellationToken.None);

        Assert.Equal(CreateUserInvitationOutcome.Sent, result.Outcome);
        _invitationRepository.Verify(r => r.Add(It.IsAny<StoreEmployeeInvitation>()), Times.Never);
        Assert.NotEqual("old-hash", existing.TokenHash);
    }
}
