using Application.Abstractions;
using Application.ShoppingLists.Commands.RemoveShoppingListItem;
using Domain.ShoppingLists;
using Moq;

namespace Application.Tests;

public class RemoveShoppingListItemCommandHandlerTests
{
    private const string OwnerId = "user-1";
    private const int ListId = 1;

    private readonly Mock<IShoppingListRepository> _shoppingListRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RemoveShoppingListItemCommandHandler CreateHandler() => new(_shoppingListRepository.Object, _unitOfWork.Object);

    private static ShoppingList CreateList(int itemId = 7) => new()
    {
        UserId = OwnerId,
        Name = "List",
        CreatedAt = DateTimeOffset.UtcNow,
        Items = [new ShoppingListItem { Id = itemId, ProductId = 1, Quantity = 2 }]
    };

    [Fact]
    public async Task Handle_ListNotFound_ReturnsListNotFound()
    {
        _shoppingListRepository.Setup(r => r.GetByIdAsync(ListId, It.IsAny<CancellationToken>())).ReturnsAsync((ShoppingList?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new RemoveShoppingListItemCommand(ListId, 7, OwnerId), CancellationToken.None);

        Assert.Equal(RemoveShoppingListItemOutcome.ListNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _shoppingListRepository.Setup(r => r.GetByIdAsync(ListId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateList());

        var handler = CreateHandler();
        var result = await handler.Handle(new RemoveShoppingListItemCommand(ListId, 7, "someone-else"), CancellationToken.None);

        Assert.Equal(RemoveShoppingListItemOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ItemDoesNotExist_ReturnsItemNotFound()
    {
        _shoppingListRepository.Setup(r => r.GetByIdAsync(ListId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateList());

        var handler = CreateHandler();
        var result = await handler.Handle(new RemoveShoppingListItemCommand(ListId, 999, OwnerId), CancellationToken.None);

        Assert.Equal(RemoveShoppingListItemOutcome.ItemNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidItem_RemovesIt()
    {
        var list = CreateList();
        _shoppingListRepository.Setup(r => r.GetByIdAsync(ListId, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var handler = CreateHandler();
        var result = await handler.Handle(new RemoveShoppingListItemCommand(ListId, 7, OwnerId), CancellationToken.None);

        Assert.Equal(RemoveShoppingListItemOutcome.Removed, result.Outcome);
        Assert.Empty(list.Items);
    }
}
