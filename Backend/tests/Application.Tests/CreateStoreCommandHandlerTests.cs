using Application.Abstractions;
using Application.Stores.Commands.CreateStore;
using Domain.Stores;
using Moq;

namespace Application.Tests;

public class CreateStoreCommandHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateStoreCommandHandler CreateHandler() => new(_storeRepository.Object, _authService.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_CreatesStore_AndPromotesOwnerToStorePartner()
    {
        Store? added = null;
        _storeRepository.Setup(r => r.Add(It.IsAny<Store>())).Callback<Store>(s =>
        {
            s.Id = 3;
            added = s;
        });

        var handler = CreateHandler();
        var command = new CreateStoreCommand("user-1", "My Store", "Dushanbe, 5th st.", 38.56, 68.78);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(3, result.StoreId);
        Assert.NotNull(added);
        Assert.Equal("user-1", added!.OwnerUserId);
        Assert.Equal("My Store", added.Name);
        Assert.Equal(38.56, added.Location.Latitude);
        Assert.Equal(68.78, added.Location.Longitude);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _authService.Verify(a => a.AssignRoleAsync("user-1", "StorePartner", It.IsAny<CancellationToken>()), Times.Once);
    }
}
