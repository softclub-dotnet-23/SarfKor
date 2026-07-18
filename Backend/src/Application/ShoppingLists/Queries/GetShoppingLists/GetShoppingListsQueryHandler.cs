using Application.Abstractions;
using Application.Common;

namespace Application.ShoppingLists.Queries.GetShoppingLists;

public sealed class GetShoppingListsQueryHandler(IShoppingListRepository shoppingListRepository) : IQueryHandler<GetShoppingListsQuery, GetShoppingListsResult>
{
    public async Task<GetShoppingListsResult> Handle(GetShoppingListsQuery query, CancellationToken cancellationToken)
    {
        var lists = await shoppingListRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        var dtos = lists
            .Select(l => new ShoppingListDto(
                l.Id,
                l.Name,
                l.Items.Select(i => new ShoppingListItemDto(i.Id, i.ProductId, i.Quantity)).ToList()))
            .ToList();

        return new GetShoppingListsResult(dtos);
    }
}
