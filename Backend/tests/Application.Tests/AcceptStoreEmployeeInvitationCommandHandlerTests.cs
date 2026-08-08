using Application.Abstractions;
using Application.Common;
using Application.Stores.Commands.AcceptStoreEmployeeInvitation;
using Domain.Identity;
using Domain.Stores;
using Moq;

namespace Application.Tests;

public class AcceptStoreEmployeeInvitationCommandHandlerTests
{
    private const string Token = "invite-token-123";
    private static readonly string TokenHash = InviteToken.Hash(Token);
    private const string Email = "newcashier@sarfkor.tj";
    private const string Password = "NewCashier123!";
    private const string DisplayName = "Cashier Name";
    private const int StoreId = 1;

    private readonly Mock<IStoreEmployeeInvitationRepository> _invitationRepository = new();
    private readonly Mock<IStoreEmployeeRepository> _storeEmployeeRepository = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AcceptStoreEmployeeInvitationCommandHandler CreateHandler() =>
        new(_invitationRepository.Object, _storeEmployeeRepository.Object, _userProfileRepository.Object, _authService.Object, _unitOfWork.Object);

    private static AcceptStoreEmployeeInvitationCommand ValidCommand(string? password = Password) => new(Token, DisplayName, password);

    private static StoreEmployeeInvitation ValidInvitation(
        DateTimeOffset? expiresAt = null, StoreEmployeeInvitationStatus status = StoreEmployeeInvitationStatus.Pending) => new()
    {
        StoreId = StoreId,
        Email = Email,
        Role = StoreEmployeeRole.Cashier,
        TokenHash = TokenHash,
        InvitedByUserId = "owner-1",
        ExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddDays(1),
        CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
        LastSentAt = DateTimeOffset.UtcNow.AddHours(-1),
        Status = status
    };

    [Fact]
    public async Task Handle_UnknownToken_ReturnsNotFound()
    {
        _invitationRepository.Setup(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>())).ReturnsAsync((StoreEmployeeInvitation?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AcceptStoreEmployeeInvitationOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsExpired()
    {
        _invitationRepository
            .Setup(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidInvitation(expiresAt: DateTimeOffset.UtcNow.AddHours(-1)));

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AcceptStoreEmployeeInvitationOutcome.Expired, result.Outcome);
    }

    [Fact]
    public async Task Handle_AlreadyAcceptedToken_ReturnsAlreadyAccepted()
    {
        _invitationRepository
            .Setup(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidInvitation(status: StoreEmployeeInvitationStatus.Accepted));

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AcceptStoreEmployeeInvitationOutcome.AlreadyAccepted, result.Outcome);
    }

    [Fact]
    public async Task Handle_RevokedToken_ReturnsRevoked()
    {
        _invitationRepository
            .Setup(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidInvitation(status: StoreEmployeeInvitationStatus.Revoked));

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AcceptStoreEmployeeInvitationOutcome.Revoked, result.Outcome);
    }

    [Fact]
    public async Task Handle_NewAccountWithoutPassword_ReturnsPasswordRequired()
    {
        _invitationRepository.Setup(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>())).ReturnsAsync(ValidInvitation());
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(password: null), CancellationToken.None);

        Assert.Equal(AcceptStoreEmployeeInvitationOutcome.PasswordRequired, result.Outcome);
        _authService.Verify(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NewEmail_RegistersAccountAddsEmployeeAndReturnsTokens()
    {
        _invitationRepository.Setup(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>())).ReturnsAsync(ValidInvitation());
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var registerAuth = new AuthResult("new-user-1", "stale-access-token", "stale-refresh-token", DateTimeOffset.UtcNow.AddMinutes(15));
        _authService.Setup(a => a.RegisterAsync(Email, Password, true, It.IsAny<CancellationToken>())).ReturnsAsync(new RegisterAccountResult(registerAuth, false));
        _storeEmployeeRepository.Setup(r => r.GetByStoreIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        // The tokens actually returned to the caller come from a fresh LoginAsync call, made after
        // AssignRoleAsync grants StorePartner - RegisterAsync's own tokens would predate that role
        // and strand the new cashier on onboarding instead of their store.
        var freshLoginResult = new AuthResult("new-user-1", "fresh-access-token", "fresh-refresh-token", DateTimeOffset.UtcNow.AddMinutes(15));
        _authService.Setup(a => a.LoginAsync(Email, Password, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new LoginAccountResult(freshLoginResult, false));

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AcceptStoreEmployeeInvitationOutcome.Accepted, result.Outcome);
        Assert.Equal(freshLoginResult, result.Auth);
        _storeEmployeeRepository.Verify(r => r.Add(It.Is<StoreEmployee>(e => e.UserId == "new-user-1" && e.Role == StoreEmployeeRole.Cashier)), Times.Once);
        _authService.Verify(a => a.AssignRoleAsync("new-user-1", "StorePartner", It.IsAny<CancellationToken>()), Times.Once);
        _userProfileRepository.Verify(r => r.Add(It.Is<UserProfile>(p => p.UserId == "new-user-1" && p.DisplayName == DisplayName)), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailAlreadyHasAccount_AttachesEmployeeWithoutTouchingPasswordOrReturningTokens()
    {
        _invitationRepository.Setup(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>())).ReturnsAsync(ValidInvitation());
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync("existing-user-1");
        _storeEmployeeRepository.Setup(r => r.GetByStoreIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(password: null), CancellationToken.None);

        Assert.Equal(AcceptStoreEmployeeInvitationOutcome.AccountAlreadyExisted, result.Outcome);
        Assert.Null(result.Auth);
        _authService.Verify(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _storeEmployeeRepository.Verify(r => r.Add(It.Is<StoreEmployee>(e => e.UserId == "existing-user-1")), Times.Once);
        _userProfileRepository.Verify(r => r.Add(It.IsAny<UserProfile>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RegistrationFails_ReturnsRegistrationFailed()
    {
        _invitationRepository.Setup(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>())).ReturnsAsync(ValidInvitation());
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _authService.Setup(a => a.RegisterAsync(Email, Password, true, It.IsAny<CancellationToken>())).ReturnsAsync(new RegisterAccountResult(null, false));

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AcceptStoreEmployeeInvitationOutcome.RegistrationFailed, result.Outcome);
        _storeEmployeeRepository.Verify(r => r.Add(It.IsAny<StoreEmployee>()), Times.Never);
    }

    // --- /admin/users' generalized platform-wide invites (StoreId null) ---------------------

    private static StoreEmployeeInvitation PlatformInvitation(string invitedRole) => new()
    {
        StoreId = null,
        Email = Email,
        Role = null,
        InvitedRole = invitedRole,
        TokenHash = TokenHash,
        InvitedByUserId = "admin-1",
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
        CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
        LastSentAt = DateTimeOffset.UtcNow.AddHours(-1),
        Status = StoreEmployeeInvitationStatus.Pending
    };

    [Fact]
    public async Task Handle_PlatformAdminInvite_NewAccount_GrantsAdminRoleWithoutStoreEmployee()
    {
        _invitationRepository.Setup(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>())).ReturnsAsync(PlatformInvitation("Admin"));
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var registerAuth = new AuthResult("new-admin-1", "stale-access-token", "stale-refresh-token", DateTimeOffset.UtcNow.AddMinutes(15));
        _authService.Setup(a => a.RegisterAsync(Email, Password, true, It.IsAny<CancellationToken>())).ReturnsAsync(new RegisterAccountResult(registerAuth, false));
        var freshLoginResult = new AuthResult("new-admin-1", "fresh-access-token", "fresh-refresh-token", DateTimeOffset.UtcNow.AddMinutes(15));
        _authService.Setup(a => a.LoginAsync(Email, Password, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new LoginAccountResult(freshLoginResult, false));

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AcceptStoreEmployeeInvitationOutcome.Accepted, result.Outcome);
        _authService.Verify(a => a.AssignRoleAsync("new-admin-1", "Admin", It.IsAny<CancellationToken>()), Times.Once);
        _storeEmployeeRepository.Verify(r => r.Add(It.IsAny<StoreEmployee>()), Times.Never);
        _storeEmployeeRepository.Verify(r => r.GetByStoreIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PlatformUserInvite_ExistingAccount_GrantsUserRoleWithoutTouchingPassword()
    {
        _invitationRepository.Setup(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>())).ReturnsAsync(PlatformInvitation("User"));
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync("existing-user-2");

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(password: null), CancellationToken.None);

        Assert.Equal(AcceptStoreEmployeeInvitationOutcome.AccountAlreadyExisted, result.Outcome);
        Assert.Null(result.Auth);
        _authService.Verify(a => a.AssignRoleAsync("existing-user-2", "User", It.IsAny<CancellationToken>()), Times.Once);
        _storeEmployeeRepository.Verify(r => r.Add(It.IsAny<StoreEmployee>()), Times.Never);
    }
}
