using System.Security.Claims;
using Application.Common;
using Application.Engagement.Commands.AddFavorite;
using Application.Engagement.Commands.RemoveFavorite;
using Application.Engagement.Queries.GetFavorites;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
[EnableRateLimiting("contributions")]
public sealed class FavoritesController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Add(
        FavoriteRequest request,
        [FromServices] ICommandHandler<AddFavoriteCommand, AddFavoriteResult> handler,
        [FromServices] IValidator<AddFavoriteCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new AddFavoriteCommand(userId, request.Type, request.EntityId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            AddFavoriteOutcome.Added => Ok(result),
            AddFavoriteOutcome.EntityNotFound => NotFound(),
            _ => Problem()
        };
    }

    // DELETE requests can't carry an inferred JSON body — type/entityId come from the query string instead.
    [HttpDelete]
    public async Task<IActionResult> Remove(
        Domain.Engagement.FavoriteType type,
        int entityId,
        [FromServices] ICommandHandler<RemoveFavoriteCommand, RemoveFavoriteResult> handler,
        [FromServices] IValidator<RemoveFavoriteCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new RemoveFavoriteCommand(userId, type, entityId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            RemoveFavoriteOutcome.Removed => Ok(result),
            RemoveFavoriteOutcome.NotFound => NotFound(),
            _ => Problem()
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromServices] IQueryHandler<GetFavoritesQuery, GetFavoritesResult> handler,
        [FromServices] IValidator<GetFavoritesQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetFavoritesQuery(userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }
}
