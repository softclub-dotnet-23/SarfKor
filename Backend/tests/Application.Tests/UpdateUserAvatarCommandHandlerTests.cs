using Application.Abstractions;
using Application.Identity.Commands.UpdateUserAvatar;
using Domain.Identity;
using Moq;

namespace Application.Tests;

public class UpdateUserAvatarCommandHandlerTests
{
    private const string UserId = "user-1";

    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateUserAvatarCommandHandler CreateHandler() => new(_userProfileRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_NoExistingProfile_CreatesOneWithAvatarAndNoPreviousReference()
    {
        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        _userProfileRepository.Setup(r => r.Add(It.IsAny<UserProfile>())).Callback<UserProfile>(p => p.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateUserAvatarCommand(UserId, "a.jpg"), CancellationToken.None);

        Assert.Equal(1, result.UserProfileId);
        Assert.Null(result.PreviousAvatarReference);
        _userProfileRepository.Verify(r => r.Add(It.Is<UserProfile>(p => p.AvatarReference == "a.jpg" && p.UserId == UserId)), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingProfile_ReplacesReferenceAndReturnsOldOneForCleanup()
    {
        var existing = new UserProfile { UserId = UserId, DisplayName = "Aziz", AvatarReference = "old.png", PreferredLanguage = "ru" };
        existing.Id = 7;
        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateUserAvatarCommand(UserId, "new.jpg"), CancellationToken.None);

        Assert.Equal(7, result.UserProfileId);
        Assert.Equal("old.png", result.PreviousAvatarReference);
        Assert.Equal("new.jpg", existing.AvatarReference);
        // Other fields are untouched by an avatar-only update.
        Assert.Equal("Aziz", existing.DisplayName);
        _userProfileRepository.Verify(r => r.Add(It.IsAny<UserProfile>()), Times.Never);
    }
}
