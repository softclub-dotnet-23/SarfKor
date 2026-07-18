using Application.Abstractions;
using Application.ShoppingLists.Commands.CreateShoppingList;
using Domain.ShoppingLists;
using Moq;

namespace Application.Tests;

public class CreateShoppingListCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesShoppingList_AndReturnsItsId()
    {
        var repo = new Mock<IShoppingListRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.Add(It.IsAny<ShoppingList>())).Callback<ShoppingList>(l => l.Id = 1);

        var handler = new CreateShoppingListCommandHandler(repo.Object, uow.Object);
        var result = await handler.Handle(new CreateShoppingListCommand("user-1", "Weekly groceries"), CancellationToken.None);

        Assert.Equal(1, result.ShoppingListId);
        repo.Verify(r => r.Add(It.Is<ShoppingList>(l => l.UserId == "user-1" && l.Name == "Weekly groceries")), Times.Once);
    }
}
