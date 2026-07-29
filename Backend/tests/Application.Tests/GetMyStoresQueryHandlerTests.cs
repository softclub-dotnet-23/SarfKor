using Application.Abstractions;
using Application.Stores.Queries.GetMyStores;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class GetMyStoresQueryHandlerTests
{
    private const string UserId = "user-1";

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreEmployeeRepository> _storeEmployeeRepository = new();

    private GetMyStoresQueryHandler CreateHandler() => new(_storeRepository.Object, _storeEmployeeRepository.Object);

    private static Store MakeStore(int id, string ownerUserId, string name) =>
        new() { Id = id, OwnerUserId = ownerUserId, Name = name, Address = "Addr", Location = new GeoLocation(0, 0) };

    [Fact]
    public async Task Handle_NoStoresOwnedOrEmployed_ReturnsEmptyList()
    {
        _storeRepository.Setup(r => r.GetOwnedByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _storeEmployeeRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetMyStoresQuery(UserId), CancellationToken.None);

        Assert.Empty(result.Stores);
    }

    [Fact]
    public async Task Handle_OwnedStore_ReturnsItWithOwnerRole()
    {
        _storeRepository.Setup(r => r.GetOwnedByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeStore(1, UserId, "My Store")]);
        _storeEmployeeRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetMyStoresQuery(UserId), CancellationToken.None);

        var dto = Assert.Single(result.Stores);
        Assert.Equal(1, dto.StoreId);
        Assert.Equal("My Store", dto.Name);
        Assert.Equal("Owner", dto.Role);
    }

    [Fact]
    public async Task Handle_EmployedStore_ReturnsItWithEmployeeRole()
    {
        _storeRepository.Setup(r => r.GetOwnedByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _storeEmployeeRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new StoreEmployee { StoreId = 2, UserId = UserId, Role = StoreEmployeeRole.Cashier, AddedAt = DateTimeOffset.UtcNow }]);
        _storeRepository.Setup(r => r.GetByIdsAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(2)), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeStore(2, "owner-2", "Other Store")]);

        var result = await CreateHandler().Handle(new GetMyStoresQuery(UserId), CancellationToken.None);

        var dto = Assert.Single(result.Stores);
        Assert.Equal(2, dto.StoreId);
        Assert.Equal("Other Store", dto.Name);
        Assert.Equal("Cashier", dto.Role);
    }

    [Fact]
    public async Task Handle_OwnedAndEmployedElsewhere_ReturnsBothWithoutDuplicates()
    {
        _storeRepository.Setup(r => r.GetOwnedByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeStore(1, UserId, "My Store")]);
        _storeEmployeeRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new StoreEmployee { StoreId = 2, UserId = UserId, Role = StoreEmployeeRole.Cashier, AddedAt = DateTimeOffset.UtcNow }]);
        _storeRepository.Setup(r => r.GetByIdsAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(2)), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeStore(2, "owner-2", "Other Store")]);

        var result = await CreateHandler().Handle(new GetMyStoresQuery(UserId), CancellationToken.None);

        Assert.Equal(2, result.Stores.Count);
        Assert.Contains(result.Stores, s => s.StoreId == 1 && s.Role == "Owner");
        Assert.Contains(result.Stores, s => s.StoreId == 2 && s.Role == "Cashier");
    }
}
