using System.Security.Claims;
using Application.Common;
using Application.ShoppingLists.Commands.AddShoppingListItem;
using Application.ShoppingLists.Commands.CreateShoppingList;
using Application.ShoppingLists.Commands.RemoveShoppingListItem;
using Application.ShoppingLists.Queries.GetShoppingLists;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/shopping-lists")]
[Authorize]
public sealed class ShoppingListsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateShoppingListRequest request,
        [FromServices] ICommandHandler<CreateShoppingListCommand, CreateShoppingListResult> handler,
        [FromServices] IValidator<CreateShoppingListCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new CreateShoppingListCommand(userId, request.Name);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(command, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromServices] IQueryHandler<GetShoppingListsQuery, GetShoppingListsResult> handler,
        [FromServices] IValidator<GetShoppingListsQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetShoppingListsQuery(userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpPost("{listId:int}/items")]
    public async Task<IActionResult> AddItem(
        int listId,
        AddShoppingListItemRequest request,
        [FromServices] ICommandHandler<AddShoppingListItemCommand, AddShoppingListItemResult> handler,
        [FromServices] IValidator<AddShoppingListItemCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new AddShoppingListItemCommand(listId, userId, request.ProductId, request.Quantity);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            AddShoppingListItemOutcome.Added => Ok(result),
            AddShoppingListItemOutcome.ListNotFound => NotFound(),
            AddShoppingListItemOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpDelete("{listId:int}/items/{itemId:int}")]
    public async Task<IActionResult> RemoveItem(
        int listId,
        int itemId,
        [FromServices] ICommandHandler<RemoveShoppingListItemCommand, RemoveShoppingListItemResult> handler,
        [FromServices] IValidator<RemoveShoppingListItemCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new RemoveShoppingListItemCommand(listId, itemId, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            RemoveShoppingListItemOutcome.Removed => Ok(result),
            RemoveShoppingListItemOutcome.ListNotFound => NotFound("Shopping list not found."),
            RemoveShoppingListItemOutcome.ItemNotFound => NotFound("Item not found."),
            RemoveShoppingListItemOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }
}
