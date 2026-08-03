using System.Security.Claims;
using Application.Assistant.Commands.AskAssistant;
using Application.Assistant.Commands.ConfirmAssistantAction;
using Application.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

// Admin,StorePartner (OR, not a named combined policy) -- covers real store owners AND cashiers,
// who also carry the StorePartner Identity role (see AddStoreEmployeeCommandHandler). Which of the
// two a given caller actually is for a specific StoreId is resolved inside
// AskAssistantCommandHandler via StoreAccessAuthorizer/IStoreEmployeeRepository, not here.
[ApiController]
[Route("api/assistant")]
[Authorize(Roles = "Admin,StorePartner")]
[EnableRateLimiting("assistant")]
public sealed class AssistantController : ControllerBase
{
    [HttpPost("chat")]
    public async Task<IActionResult> Chat(
        AssistantChatRequest request,
        [FromServices] ICommandHandler<AskAssistantCommand, AskAssistantResult> handler,
        [FromServices] IValidator<AskAssistantCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        // UserId/role flags come from JWT claims, never the request body -- StoreId is the only
        // caller-supplied identity-adjacent value, and it's re-verified against real store
        // ownership/employment inside the handler before anything else happens.
        var command = new AskAssistantCommand(
            userId,
            User.IsInRole("Admin"),
            User.IsInRole("StorePartner"),
            request.StoreId,
            request.History?.Select(m => new AssistantChatMessage(m.Role, m.Content)).ToList() ?? [],
            request.Message);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            AskAssistantOutcome.Answered => Ok(result),
            AskAssistantOutcome.StoreNotFound => NotFound("Store not found."),
            AskAssistantOutcome.Forbidden => Forbid(),
            _ => Problem(),
        };
    }

    [HttpPost("actions/{pendingActionId:int}/confirm")]
    public async Task<IActionResult> ConfirmAction(
        int pendingActionId,
        [FromServices] ICommandHandler<ConfirmAssistantActionCommand, ConfirmAssistantActionResult> handler,
        [FromServices] IValidator<ConfirmAssistantActionCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var command = new ConfirmAssistantActionCommand(pendingActionId, userId);
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ConfirmAssistantActionOutcome.Confirmed => Ok(result),
            // Same 200 shape as a fresh confirm -- a retried request must look identical to the
            // caller, not like a different (or failed) outcome.
            ConfirmAssistantActionOutcome.AlreadyConfirmed => Ok(result),
            ConfirmAssistantActionOutcome.NotFound => NotFound("Pending action not found."),
            ConfirmAssistantActionOutcome.Forbidden => Forbid(),
            ConfirmAssistantActionOutcome.Expired => Conflict("This proposal has expired."),
            ConfirmAssistantActionOutcome.FeatureDisabled => Conflict("Assistant actions are currently disabled."),
            ConfirmAssistantActionOutcome.ExecutionFailed => UnprocessableEntity(result.Summary ?? "Failed to execute the action."),
            _ => Problem(),
        };
    }
}
