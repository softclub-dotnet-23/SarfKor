using Application.Abstractions;
using Application.Identity.Commands.RecordUserConsent;
using Domain.Identity;
using Moq;

namespace Application.Tests;

public class RecordUserConsentCommandHandlerTests
{
    private const string UserId = "user-1";

    private readonly Mock<IUserConsentRepository> _userConsentRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RecordUserConsentCommandHandler CreateHandler() => new(_userConsentRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_NoExistingConsent_CreatesNewRow()
    {
        _userConsentRepository
            .Setup(r => r.GetByUserIdAndTypeAsync(UserId, ConsentType.Geolocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserConsent?)null);
        _userConsentRepository.Setup(r => r.Add(It.IsAny<UserConsent>())).Callback<UserConsent>(c => c.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(new RecordUserConsentCommand(UserId, ConsentType.Geolocation, true), CancellationToken.None);

        Assert.Equal(1, result.UserConsentId);
        _userConsentRepository.Verify(r => r.Add(It.Is<UserConsent>(c => c.IsGranted)), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingConsent_FlipsGrantedFlagInPlace()
    {
        var existing = new UserConsent { UserId = UserId, Type = ConsentType.Marketing, IsGranted = true, RecordedAt = DateTimeOffset.UtcNow.AddDays(-10) };
        existing.Id = 3;
        _userConsentRepository
            .Setup(r => r.GetByUserIdAndTypeAsync(UserId, ConsentType.Marketing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        var result = await handler.Handle(new RecordUserConsentCommand(UserId, ConsentType.Marketing, false), CancellationToken.None);

        Assert.Equal(3, result.UserConsentId);
        Assert.False(existing.IsGranted);
        _userConsentRepository.Verify(r => r.Add(It.IsAny<UserConsent>()), Times.Never);
    }
}
