using Application.Abstractions;
using Application.Stores.Commands.AdminCreateStorePartner;
using Domain.Stores;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests;

public class AdminCreateStorePartnerCommandHandlerTests
{
    private const string AdminUserId = "admin-1";
    private const string Email = "newpartner@sarfkor.tj";

    private readonly Mock<IStoreOwnerInvitationRepository> _invitationRepository = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<AdminCreateStorePartnerCommandHandler>> _logger = new();

    private AdminCreateStorePartnerCommandHandler CreateHandler() => new(
        _invitationRepository.Object, _authService.Object, _emailSender.Object, _auditLogRepository.Object, _unitOfWork.Object, _logger.Object);

    private static AdminCreateStorePartnerCommand ValidCommand() => new(AdminUserId, Email, "New Store", "Dushanbe", 38.5, 68.7);

    [Fact]
    public async Task Handle_EmailAlreadyRegistered_ReturnsEmailAlreadyRegistered()
    {
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync("existing-user");

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AdminCreateStorePartnerOutcome.EmailAlreadyRegistered, result.Outcome);
        _invitationRepository.Verify(r => r.Add(It.IsAny<StoreOwnerInvitation>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoExistingInvitation_CreatesOneAndSendsEmail()
    {
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _invitationRepository.Setup(r => r.GetPendingByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync((StoreOwnerInvitation?)null);
        _invitationRepository.Setup(r => r.Add(It.IsAny<StoreOwnerInvitation>())).Callback<StoreOwnerInvitation>(i => i.Id = 5);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AdminCreateStorePartnerOutcome.Invited, result.Outcome);
        Assert.Equal(5, result.InvitationId);
        _invitationRepository.Verify(r => r.Add(It.Is<StoreOwnerInvitation>(i => i.Email == Email && i.StoreName == "New Store")), Times.Once);
        _emailSender.Verify(e => e.SendStoreOwnerInvitationEmailAsync(Email, "New Store", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingPendingInvitation_ReusesRowWithFreshCodeAndResetAttempts()
    {
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var existing = new StoreOwnerInvitation
        {
            Id = 9,
            Email = Email,
            StoreName = "Old Name",
            Address = "Old Address",
            Location = new GeoLocation(0, 0),
            CodeHash = "old-hash",
            AttemptCount = 3,
            InvitedByUserId = AdminUserId,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _invitationRepository.Setup(r => r.GetPendingByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AdminCreateStorePartnerOutcome.Invited, result.Outcome);
        Assert.Equal(9, result.InvitationId);
        Assert.Equal("New Store", existing.StoreName);
        Assert.Equal(0, existing.AttemptCount);
        Assert.NotEqual("old-hash", existing.CodeHash);
        _invitationRepository.Verify(r => r.Add(It.IsAny<StoreOwnerInvitation>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailSendThrows_IsSwallowedAndStillReturnsInvited()
    {
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _invitationRepository.Setup(r => r.GetPendingByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync((StoreOwnerInvitation?)null);
        _emailSender
            .Setup(e => e.SendStoreOwnerInvitationEmailAsync(Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP not configured"));

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AdminCreateStorePartnerOutcome.Invited, result.Outcome);
    }
}
