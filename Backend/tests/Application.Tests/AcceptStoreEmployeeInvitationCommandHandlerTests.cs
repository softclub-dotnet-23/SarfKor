using Application.Abstractions;
using Application.Stores.Commands.AcceptStoreEmployeeInvitation;
using Domain.Stores;
using Moq;

namespace Application.Tests;

public class AcceptStoreEmployeeInvitationCommandHandlerTests
{
    private const string Token = "invite-token-123";
    private const string Email = "newcashier@sarfkor.tj";
    private const string Password = "NewCashier123!";
    private const int StoreId = 1;

    private readonly Mock<IStoreEmployeeInvitationRepository> _invitationRepository = new();
    private readonly Mock<IStoreEmployeeRepository> _storeEmployeeRepository = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AcceptStoreEmployeeInvitationCommandHandler CreateHandler() =>
        new(_invitationRepository.Object, _storeEmployeeRepository.Object, _authService.Object, _unitOfWork.Object);

    private static AcceptStoreEmployeeInvitationCommand ValidCommand() => new(Token, Password);

    private static StoreEmployeeInvitation ValidInvitation(DateTimeOffset? expiresAt = null, DateTimeOffset? acceptedAt = null) => new()
    {
        StoreId = StoreId,
        Email = Email,
        Role = StoreEmployeeRole.Cashier,
        Token = Token,
        InvitedByUserId = "owner-1",
        ExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddHours(1),
        AcceptedAt = acceptedAt,
        CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
    };

    [Fact]
    public async Task Handle_UnknownToken_ReturnsInvalidOrExpiredToken()
    {
        _invitationRepository.Setup(r => r.GetByTokenAsync(Token, It.IsAny<CancellationToken>())).ReturnsAsync((StoreEmployeeInvitation?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AcceptStoreEmployeeInvitationOutcome.InvalidOrExpiredToken, result.Outcome);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsInvalidOrExpiredToken()
    {
        _invitationRepository
            .Setup(r => r.GetByTokenAsync(Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidInvitation(expiresAt: DateTimeOffset.UtcNow.AddHours(-1)));

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AcceptStoreEmployeeInvitationOutcome.InvalidOrExpiredToken, result.Outcome);
    }

    [Fact]
    public async Task Handle_AlreadyAcceptedToken_ReturnsInvalidOrExpiredToken()
    {
        _invitationRepository
            .Setup(r => r.GetByTokenAsync(Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidInvitation(acceptedAt: DateTimeOffset.UtcNow.AddMinutes(-5)));

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AcceptStoreEmployeeInvitationOutcome.InvalidOrExpiredToken, result.Outcome);
    }

    [Fact]
    public async Task Handle_NewEmail_RegistersAccountAddsEmployeeAndReturnsTokens()
    {
        _invitationRepository.Setup(r => r.GetByTokenAsync(Token, It.IsAny<CancellationToken>())).ReturnsAsync(ValidInvitation());
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
    }

    [Fact]
    public async Task Handle_EmailAlreadyHasAccount_AttachesEmployeeWithoutTouchingPasswordOrReturningTokens()
    {
        _invitationRepository.Setup(r => r.GetByTokenAsync(Token, It.IsAny<CancellationToken>())).ReturnsAsync(ValidInvitation());
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync("existing-user-1");
        _storeEmployeeRepository.Setup(r => r.GetByStoreIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AcceptStoreEmployeeInvitationOutcome.AccountAlreadyExisted, result.Outcome);
        Assert.Null(result.Auth);
        _authService.Verify(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _storeEmployeeRepository.Verify(r => r.Add(It.Is<StoreEmployee>(e => e.UserId == "existing-user-1")), Times.Once);
    }

    [Fact]
    public async Task Handle_RegistrationFails_ReturnsRegistrationFailed()
    {
        _invitationRepository.Setup(r => r.GetByTokenAsync(Token, It.IsAny<CancellationToken>())).ReturnsAsync(ValidInvitation());
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _authService.Setup(a => a.RegisterAsync(Email, Password, true, It.IsAny<CancellationToken>())).ReturnsAsync(new RegisterAccountResult(null, false));

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AcceptStoreEmployeeInvitationOutcome.RegistrationFailed, result.Outcome);
        _storeEmployeeRepository.Verify(r => r.Add(It.IsAny<StoreEmployee>()), Times.Never);
    }
}
