using Application.Abstractions;
using Application.ShoppingLists.Commands.AddShoppingListItem;
using Domain.ShoppingLists;
using Moq;

namespace Application.Tests;

public class AddShoppingListItemCommandHandlerTests
{
    private const string OwnerId = "user-1";
    private const int ListId = 1;

    private readonly Mock<IShoppingListRepository> _shoppingListRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AddShoppingListItemCommandHandler CreateHandler() => new(_shoppingListRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_ListNotFound_ReturnsListNotFound()
    {
        _shoppingListRepository.Setup(r => r.GetByIdAsync(ListId, It.IsAny<CancellationToken>())).ReturnsAsync((ShoppingList?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new AddShoppingListItemCommand(ListId, OwnerId, 1, 2), CancellationToken.None);

        Assert.Equal(AddShoppingListItemOutcome.ListNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _shoppingListRepository
            .Setup(r => r.GetByIdAsync(ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShoppingList { UserId = OwnerId, Name = "List", CreatedAt = DateTimeOffset.UtcNow });

        var handler = CreateHandler();
        var result = await handler.Handle(new AddShoppingListItemCommand(ListId, "someone-else", 1, 2), CancellationToken.None);

        Assert.Equal(AddShoppingListItemOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_Owner_AddsItem()
    {
        var list = new ShoppingList { UserId = OwnerId, Name = "List", CreatedAt = DateTimeOffset.UtcNow };
        _shoppingListRepository.Setup(r => r.GetByIdAsync(ListId, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var handler = CreateHandler();
        var result = await handler.Handle(new AddShoppingListItemCommand(ListId, OwnerId, 5, 3), CancellationToken.None);

        Assert.Equal(AddShoppingListItemOutcome.Added, result.Outcome);
        Assert.Single(list.Items);
        Assert.Equal(5, list.Items[0].ProductId);
        Assert.Equal(3, list.Items[0].Quantity);
    }
}
