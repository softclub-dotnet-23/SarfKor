using Application.Abstractions;
using Application.Common;

namespace Application.ShoppingLists.Commands.RemoveShoppingListItem;

public sealed class RemoveShoppingListItemCommandHandler(
    IShoppingListRepository shoppingListRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveShoppingListItemCommand, RemoveShoppingListItemResult>
{
    public async Task<RemoveShoppingListItemResult> Handle(RemoveShoppingListItemCommand command, CancellationToken cancellationToken)
    {
        var list = await shoppingListRepository.GetByIdAsync(command.ShoppingListId, cancellationToken);
        if (list is null)
            return new RemoveShoppingListItemResult(RemoveShoppingListItemOutcome.ListNotFound);

        if (list.UserId != command.UserId)
            return new RemoveShoppingListItemResult(RemoveShoppingListItemOutcome.Forbidden);

        var item = list.Items.FirstOrDefault(i => i.Id == command.ItemId);
        if (item is null)
            return new RemoveShoppingListItemResult(RemoveShoppingListItemOutcome.ItemNotFound);

        list.Items.Remove(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RemoveShoppingListItemResult(RemoveShoppingListItemOutcome.Removed);
    }
}
