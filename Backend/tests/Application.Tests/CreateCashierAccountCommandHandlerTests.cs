using Application.Abstractions;
using Application.Common;
using Application.Stores.Commands.CreateCashierAccount;
using Domain.Stores;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests;

public class CreateCashierAccountCommandHandlerTests
{
    private const string OwnerUserId = "owner-1";
    private const string NewUserId = "new-cashier-1";
    private const string Email = "cashier@sarfkor.tj";
    private const string DisplayName = "Кассир Тестовый";
    private const int StoreId = 5;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IStoreEmployeeRepository> _storeEmployeeRepository = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateCashierAccountCommandHandler CreateHandler() => new(
        _storeRepository.Object,
        _storeAccessAuthorizer.Object,
        _storeEmployeeRepository.Object,
        _userProfileRepository.Object,
        _authService.Object,
        _auditLogRepository.Object,
        _unitOfWork.Object,
        new LoggerFactory().CreateLogger<CreateCashierAccountCommandHandler>());

    private static Store ValidStore() => new()
    {
        OwnerUserId = OwnerUserId,
        Name = "Тестовый магазин",
        Address = "ул. Тестовая, 1",
        Location = new GeoLocation(38.5, 68.7),
        Status = StoreStatus.Active,
    };

    private static CreateCashierAccountCommand ValidCommand() => new(StoreId, Email, DisplayName, OwnerUserId);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreateCashierAccountOutcome.StoreNotFound, result.Outcome);
        _authService.Verify(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CallerNotOwner_ReturnsForbidden()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(ValidStore());
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerUserId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreateCashierAccountOutcome.Forbidden, result.Outcome);
        _authService.Verify(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailAlreadyRegistered_ReturnsEmailAlreadyRegisteredAndNeverTouchesPassword()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(ValidStore());
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync("some-existing-user");

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreateCashierAccountOutcome.EmailAlreadyRegistered, result.Outcome);
        Assert.Null(result.Password);
        _authService.Verify(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _storeEmployeeRepository.Verify(r => r.Add(It.IsAny<StoreEmployee>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Valid_CreatesAccountAsCashierAndReturnsOneTimePassword()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(ValidStore());
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _authService.Setup(a => a.FindUserIdByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var registerAuth = new AuthResult(NewUserId, "access-token", "refresh-token", DateTimeOffset.UtcNow.AddMinutes(15));
        _authService
            .Setup(a => a.RegisterAsync(Email, It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterAccountResult(registerAuth, false));

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreateCashierAccountOutcome.Created, result.Outcome);
        Assert.Equal(Email, result.Email);
        Assert.NotNull(result.Password);
        // Not asserting an exact value (GeneratedPassword.Generate() is random by design) -- just
        // that it actually satisfies the same complexity policy ASP.NET Identity enforces, since a
        // password this handler hands back that Identity would itself reject is a self-inflicted bug.
        Assert.True(result.Password!.Length >= 12);
        Assert.Contains(result.Password, c => char.IsUpper(c));
        Assert.Contains(result.Password, c => char.IsLower(c));
        Assert.Contains(result.Password, c => char.IsDigit(c));
        Assert.Contains(result.Password, c => !char.IsLetterOrDigit(c));

        _storeEmployeeRepository.Verify(
            r => r.Add(It.Is<StoreEmployee>(e => e.StoreId == StoreId && e.UserId == NewUserId && e.Role == StoreEmployeeRole.Cashier)),
            Times.Once);
        _authService.Verify(a => a.AssignRoleAsync(NewUserId, "StorePartner", It.IsAny<CancellationToken>()), Times.Once);
        _userProfileRepository.Verify(r => r.Add(It.Is<Domain.Identity.UserProfile>(p => p.UserId == NewUserId && p.DisplayName == DisplayName)), Times.Once);
        _auditLogRepository.Verify(
            r => r.Add(It.Is<Domain.Auditing.AuditLog>(a => a.Action == "CashierAccount.Created" && a.PerformedByUserId == OwnerUserId)),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public void GeneratedPassword_AlwaysMeetsComplexityPolicy()
    {
        // Regression guard on the generator itself, independent of the handler -- 200 samples is
        // cheap and would have caught the "guaranteed-class-chars always land at fixed positions"
        // class of bug if the shuffle step were ever removed.
        for (var i = 0; i < 200; i++)
        {
            var password = GeneratedPassword.Generate();
            Assert.True(password.Length >= 12);
            Assert.Contains(password, char.IsUpper);
            Assert.Contains(password, char.IsLower);
            Assert.Contains(password, char.IsDigit);
            Assert.Contains(password, c => !char.IsLetterOrDigit(c));
        }
    }
}
