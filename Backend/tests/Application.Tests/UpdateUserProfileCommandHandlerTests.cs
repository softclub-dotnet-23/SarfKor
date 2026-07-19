using Application.Abstractions;
using Application.Identity.Commands.UpdateUserProfile;
using Domain.Identity;
using Moq;

namespace Application.Tests;

public class UpdateUserProfileCommandHandlerTests
{
    private const string UserId = "user-1";

    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateUserProfileCommandHandler CreateHandler() => new(_userProfileRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_NoExistingProfile_CreatesNewOne()
    {
        _userProfileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync((UserProfile?)null);
        _userProfileRepository.Setup(r => r.Add(It.IsAny<UserProfile>())).Callback<UserProfile>(p => p.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateUserProfileCommand(UserId, "Ali", null, "tg"), CancellationToken.None);

        Assert.Equal(1, result.UserProfileId);
        _userProfileRepository.Verify(r => r.Add(It.Is<UserProfile>(p => p.DisplayName == "Ali" && p.PreferredLanguage == "tg")), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingProfile_UpdatesInPlaceRatherThanDuplicating()
    {
        var existing = new UserProfile { UserId = UserId, DisplayName = "Old Name", PreferredLanguage = "ru" };
        existing.Id = 5;
        _userProfileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateUserProfileCommand(UserId, "New Name", "avatar.png", "tg"), CancellationToken.None);

        Assert.Equal(5, result.UserProfileId);
        Assert.Equal("New Name", existing.DisplayName);
        Assert.Equal("avatar.png", existing.AvatarReference);
        Assert.Equal("tg", existing.PreferredLanguage);
        _userProfileRepository.Verify(r => r.Add(It.IsAny<UserProfile>()), Times.Never);
    }
}
