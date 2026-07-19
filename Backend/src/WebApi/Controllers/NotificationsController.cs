using System.Security.Claims;
using Application.Common;
using Application.Notifications.Commands.MarkNotificationAsRead;
using Application.Notifications.Queries.GetNotifications;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromServices] IQueryHandler<GetNotificationsQuery, GetNotificationsResult> handler,
        [FromServices] IValidator<GetNotificationsQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetNotificationsQuery(userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpPost("{notificationId:int}/read")]
    public async Task<IActionResult> MarkAsRead(
        int notificationId,
        [FromServices] ICommandHandler<MarkNotificationAsReadCommand, MarkNotificationAsReadResult> handler,
        [FromServices] IValidator<MarkNotificationAsReadCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new MarkNotificationAsReadCommand(notificationId, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            MarkNotificationAsReadOutcome.MarkedRead => Ok(result),
            MarkNotificationAsReadOutcome.NotFound => NotFound(),
            MarkNotificationAsReadOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }
}
