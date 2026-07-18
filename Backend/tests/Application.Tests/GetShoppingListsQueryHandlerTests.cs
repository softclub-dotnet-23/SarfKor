using Application.Abstractions;
using Application.ShoppingLists.Queries.GetShoppingLists;
using Domain.ShoppingLists;
using Moq;

namespace Application.Tests;

public class GetShoppingListsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsListsWithItems()
    {
        var repo = new Mock<IShoppingListRepository>();
        repo.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync([
            new ShoppingList
            {
                UserId = "user-1",
                Name = "Weekly",
                CreatedAt = DateTimeOffset.UtcNow,
                Items = [new ShoppingListItem { Id = 1, ProductId = 5, Quantity = 2 }]
            }
        ]);

        var handler = new GetShoppingListsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetShoppingListsQuery("user-1"), CancellationToken.None);

        Assert.Single(result.Lists);
        Assert.Equal("Weekly", result.Lists[0].Name);
        Assert.Single(result.Lists[0].Items);
        Assert.Equal(5, result.Lists[0].Items[0].ProductId);
    }
}
