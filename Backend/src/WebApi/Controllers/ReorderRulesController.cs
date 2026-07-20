using System.Security.Claims;
using Application.Common;
using Application.Inventory.Commands.CreateReorderRule;
using Application.Inventory.Queries.GetReorderAlerts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api")]
[Authorize("StorePartner")]
public sealed class ReorderRulesController : ControllerBase
{
    [HttpPost("reorder-rules")]
    public async Task<IActionResult> Create(
        CreateReorderRuleRequest request,
        [FromServices] ICommandHandler<CreateReorderRuleCommand, CreateReorderRuleResult> handler,
        [FromServices] IValidator<CreateReorderRuleCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new CreateReorderRuleCommand(request.StoreId, request.ProductId, request.ThresholdQuantity, request.ReorderQuantity, request.PreferredSupplierId, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            CreateReorderRuleOutcome.Created => Ok(result),
            CreateReorderRuleOutcome.StoreNotFound => NotFound("Store not found."),
            CreateReorderRuleOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpGet("stores/{storeId:int}/reorder-alerts")]
    public async Task<IActionResult> GetAlerts(
        int storeId,
        [FromServices] IQueryHandler<GetReorderAlertsQuery, GetReorderAlertsResult> handler,
        [FromServices] IValidator<GetReorderAlertsQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetReorderAlertsQuery(storeId, userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome switch
        {
            GetReorderAlertsOutcome.Found => Ok(result),
            GetReorderAlertsOutcome.StoreNotFound => NotFound(),
            GetReorderAlertsOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }
}
