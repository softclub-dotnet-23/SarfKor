using Application.Abstractions;
using Application.Common;
using Domain.ShoppingLists;

namespace Application.ShoppingLists.Commands.AddShoppingListItem;

public sealed class AddShoppingListItemCommandHandler(
    IShoppingListRepository shoppingListRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<AddShoppingListItemCommand, AddShoppingListItemResult>
{
    public async Task<AddShoppingListItemResult> Handle(AddShoppingListItemCommand command, CancellationToken cancellationToken)
    {
        var list = await shoppingListRepository.GetByIdAsync(command.ShoppingListId, cancellationToken);
        if (list is null)
            return new AddShoppingListItemResult(AddShoppingListItemOutcome.ListNotFound, null);

        if (list.UserId != command.UserId)
            return new AddShoppingListItemResult(AddShoppingListItemOutcome.Forbidden, null);

        var item = new ShoppingListItem { ShoppingListId = list.Id, ProductId = command.ProductId, Quantity = command.Quantity };
        list.Items.Add(item);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddShoppingListItemResult(AddShoppingListItemOutcome.Added, item.Id);
    }
}
