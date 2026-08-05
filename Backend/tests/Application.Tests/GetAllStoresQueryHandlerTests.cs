using Application.Abstractions;
using Application.Stores.Queries.GetAllStores;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class GetAllStoresQueryHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IAuthService> _authService = new();

    private GetAllStoresQueryHandler CreateHandler() => new(_storeRepository.Object, _authService.Object);

    [Fact]
    public async Task Handle_PagesThroughToRepository()
    {
        _storeRepository.Setup(r => r.GetAllAsync(20, 10, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _storeRepository.Setup(r => r.CountAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(37);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetAllStoresQuery(20, 10), CancellationToken.None);

        Assert.Equal(37, result.TotalCount);
        _storeRepository.Verify(r => r.GetAllAsync(20, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ResolvesOwnerEmailsInOneBatchedCall()
    {
        var store1 = new Store { Id = 1, OwnerUserId = "owner-1", Name = "A", Address = "Addr A", Location = new GeoLocation(0, 0), Status = StoreStatus.Active };
        var store2 = new Store { Id = 2, OwnerUserId = "owner-2", Name = "B", Address = "Addr B", Location = new GeoLocation(0, 0), Status = StoreStatus.PendingApproval };
        _storeRepository.Setup(r => r.GetAllAsync(0, 50, It.IsAny<CancellationToken>())).ReturnsAsync([store1, store2]);
        _storeRepository.Setup(r => r.CountAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _authService
            .Setup(a => a.GetEmailsByUserIdsAsync(It.Is<IReadOnlyCollection<string>>(ids => ids.Contains("owner-1") && ids.Contains("owner-2")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { ["owner-1"] = "a@sarfkor.tj" });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetAllStoresQuery(0, 50), CancellationToken.None);

        Assert.Equal(2, result.Stores.Count);
        Assert.Equal("a@sarfkor.tj", result.Stores[0].OwnerEmail);
        // owner-2 has no entry in the batched lookup — must resolve to null, not throw.
        Assert.Null(result.Stores[1].OwnerEmail);
        _authService.Verify(a => a.GetEmailsByUserIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
