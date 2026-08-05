using System.Security.Claims;
using Application.Common;
using Application.Identity.Commands.BlockUser;
using Application.Identity.Commands.UnblockUser;
using Application.Identity.Queries.GetUserDetail;
using Application.Identity.Queries.GetUsers;
using Application.Reputation.Commands.AdjustTrustScore;
using Application.Reputation.Queries.GetTrustScoreHistory;
using Application.Reputation.Queries.GetTrustScores;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize("Admin")]
[EnableRateLimiting("partner-write")]
public sealed class AdminUsersController : ControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> GetUsers(
        int? skip, int? take, string? search,
        [FromServices] IQueryHandler<GetUsersQuery, GetUsersResult> handler,
        [FromServices] IValidator<GetUsersQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetUsersQuery(skip ?? 0, take ?? 50, search);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserDetail(
        string userId,
        [FromServices] IQueryHandler<GetUserDetailQuery, GetUserDetailResult> handler,
        [FromServices] IValidator<GetUserDetailQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetUserDetailQuery(userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome == GetUserDetailOutcome.Found ? Ok(result) : NotFound();
    }

    [HttpPost("{userId}/block")]
    public async Task<IActionResult> BlockUser(
        string userId,
        BlockUserRequest request,
        [FromServices] ICommandHandler<BlockUserCommand, BlockUserResult> handler,
        [FromServices] IValidator<BlockUserCommand> validator,
        CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminUserId is null)
            return Unauthorized();

        var command = new BlockUserCommand(userId, request.Reason, adminUserId, HttpContext.Connection.RemoteIpAddress?.ToString());
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            BlockUserOutcome.Blocked => Ok(result),
            BlockUserOutcome.NotFound => NotFound(),
            _ => Problem()
        };
    }

    [HttpPost("{userId}/unblock")]
    public async Task<IActionResult> UnblockUser(
        string userId,
        UnblockUserRequest request,
        [FromServices] ICommandHandler<UnblockUserCommand, UnblockUserResult> handler,
        [FromServices] IValidator<UnblockUserCommand> validator,
        CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminUserId is null)
            return Unauthorized();

        var command = new UnblockUserCommand(userId, request.Reason, adminUserId, HttpContext.Connection.RemoteIpAddress?.ToString());
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            UnblockUserOutcome.Unblocked => Ok(result),
            UnblockUserOutcome.NotFound => NotFound(),
            _ => Problem()
        };
    }

    [HttpGet("trust-scores")]
    public async Task<IActionResult> GetTrustScores(
        int? skip, int? take,
        [FromServices] IQueryHandler<GetTrustScoresQuery, GetTrustScoresResult> handler,
        [FromServices] IValidator<GetTrustScoresQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetTrustScoresQuery(skip ?? 0, take ?? 50);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpGet("{userId}/trust-score-history")]
    public async Task<IActionResult> GetTrustScoreHistory(
        string userId,
        [FromServices] IQueryHandler<GetTrustScoreHistoryQuery, GetTrustScoreHistoryResult> handler,
        [FromServices] IValidator<GetTrustScoreHistoryQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetTrustScoreHistoryQuery(userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpPost("{userId}/trust-score/adjust")]
    public async Task<IActionResult> AdjustTrustScore(
        string userId,
        AdjustTrustScoreRequest request,
        [FromServices] ICommandHandler<AdjustTrustScoreCommand, AdjustTrustScoreResult> handler,
        [FromServices] IValidator<AdjustTrustScoreCommand> validator,
        CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminUserId is null)
            return Unauthorized();

        var command = new AdjustTrustScoreCommand(userId, request.Delta, request.Reason, adminUserId, HttpContext.Connection.RemoteIpAddress?.ToString());
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(command, cancellationToken));
    }
}
