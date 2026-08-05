using Application.Abstractions;
using Application.Common;
using Application.Stores.Commands.ConfirmStoreOwnerInvitation;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class ConfirmStoreOwnerInvitationCommandHandlerTests
{
    private const string AdminUserId = "admin-1";
    private const string Email = "newpartner@sarfkor.tj";
    private const string Code = "123456";
    private const string Password = "correct-horse-battery";

    private readonly Mock<IStoreOwnerInvitationRepository> _invitationRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ConfirmStoreOwnerInvitationCommandHandler CreateHandler() => new(
        _invitationRepository.Object, _storeRepository.Object, _authService.Object, _auditLogRepository.Object, _unitOfWork.Object);

    private static ConfirmStoreOwnerInvitationCommand ValidCommand() => new(Email, Code, Password);

    private static StoreOwnerInvitation CreateInvitation(int attemptCount = 0) => new()
    {
        Id = 1,
        Email = Email,
        StoreName = "New Store",
        Address = "Dushanbe",
        Location = new GeoLocation(38.5, 68.7),
        CodeHash = OtpCode.Hash(Email, Code),
        AttemptCount = attemptCount,
        InvitedByUserId = AdminUserId,
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
        CreatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Handle_NoPendingInvitation_ReturnsInvalidOrExpiredCode()
    {
        _invitationRepository.Setup(r => r.GetPendingByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync((StoreOwnerInvitation?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(ConfirmStoreOwnerInvitationOutcome.InvalidOrExpiredCode, result.Outcome);
    }

    [Fact]
    public async Task Handle_TooManyPriorAttempts_ReturnsTooManyAttempts()
    {
        _invitationRepository.Setup(r => r.GetPendingByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(CreateInvitation(attemptCount: 5));

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(ConfirmStoreOwnerInvitationOutcome.TooManyAttempts, result.Outcome);
    }

    [Fact]
    public async Task Handle_WrongCode_IncrementsAttemptCountAndReturnsInvalidOrExpiredCode()
    {
        var invitation = CreateInvitation();
        _invitationRepository.Setup(r => r.GetPendingByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(invitation);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand() with { Code = "000000" }, CancellationToken.None);

        Assert.Equal(ConfirmStoreOwnerInvitationOutcome.InvalidOrExpiredCode, result.Outcome);
        Assert.Equal(1, invitation.AttemptCount);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _authService.Verify(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailRaceRegisteredSinceInvite_ReturnsEmailAlreadyRegistered()
    {
        _invitationRepository.Setup(r => r.GetPendingByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(CreateInvitation());
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync("someone-else");

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(ConfirmStoreOwnerInvitationOutcome.EmailAlreadyRegistered, result.Outcome);
        _storeRepository.Verify(r => r.Add(It.IsAny<Store>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RegistrationFails_ReturnsRegistrationFailed()
    {
        _invitationRepository.Setup(r => r.GetPendingByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(CreateInvitation());
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _authService.Setup(a => a.RegisterAsync(Email, Password, true, It.IsAny<CancellationToken>())).ReturnsAsync(new RegisterAccountResult(null, false));

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(ConfirmStoreOwnerInvitationOutcome.RegistrationFailed, result.Outcome);
        _storeRepository.Verify(r => r.Add(It.IsAny<Store>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCode_CreatesApprovedStoreAssignsRoleBeforeLoggingInAndReturnsAuth()
    {
        var invitation = CreateInvitation();
        _invitationRepository.Setup(r => r.GetPendingByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(invitation);
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _authService
            .Setup(a => a.RegisterAsync(Email, Password, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterAccountResult(new AuthResult("new-user-1", "register-token", "register-refresh", DateTimeOffset.UtcNow.AddHours(1)), false));

        var callOrder = new List<string>();
        _authService
            .Setup(a => a.AssignRoleAsync("new-user-1", "StorePartner", It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AssignRole"))
            .Returns(Task.CompletedTask);
        _authService
            .Setup(a => a.LoginAsync(Email, Password, null, null, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Login"))
            .ReturnsAsync(new LoginAccountResult(new AuthResult("new-user-1", "fresh-token", "fresh-refresh", DateTimeOffset.UtcNow.AddHours(1)), false));

        Store? addedStore = null;
        _storeRepository.Setup(r => r.Add(It.IsAny<Store>())).Callback<Store>(s =>
        {
            s.Id = 42;
            addedStore = s;
        });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(ConfirmStoreOwnerInvitationOutcome.Confirmed, result.Outcome);
        Assert.Equal(42, result.StoreId);
        Assert.NotNull(result.Auth);
        Assert.Equal("fresh-token", result.Auth!.AccessToken);
        Assert.NotNull(addedStore);
        Assert.Equal("new-user-1", addedStore!.OwnerUserId);
        Assert.Equal(StoreStatus.Active, addedStore.Status);
        Assert.Equal(new[] { "AssignRole", "Login" }, callOrder);
        Assert.NotNull(invitation.AcceptedAt);
    }
}
