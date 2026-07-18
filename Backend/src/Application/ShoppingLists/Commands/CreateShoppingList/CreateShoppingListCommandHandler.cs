using Application.Abstractions;
using Application.Common;
using Domain.ShoppingLists;

namespace Application.ShoppingLists.Commands.CreateShoppingList;

public sealed class CreateShoppingListCommandHandler(
    IShoppingListRepository shoppingListRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateShoppingListCommand, CreateShoppingListResult>
{
    public async Task<CreateShoppingListResult> Handle(CreateShoppingListCommand command, CancellationToken cancellationToken)
    {
        var list = new ShoppingList { UserId = command.UserId, Name = command.Name, CreatedAt = DateTimeOffset.UtcNow };
        shoppingListRepository.Add(list);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreateShoppingListResult(list.Id);
    }
}
