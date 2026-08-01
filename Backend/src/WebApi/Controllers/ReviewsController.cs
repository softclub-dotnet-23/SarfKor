using System.Security.Claims;
using Application.Common;
using Application.Feedback.Commands.ReplyToReview;
using Application.Feedback.Commands.SubmitReview;
using Application.Feedback.Queries.GetReviews;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api")]
public sealed class ReviewsController : ControllerBase
{
    [HttpPost("products/{productId:int}/reviews")]
    [Authorize]
    [EnableRateLimiting("contributions")]
    public async Task<IActionResult> Submit(
        int productId,
        SubmitReviewRequest request,
        [FromServices] ICommandHandler<SubmitReviewCommand, SubmitReviewResult> handler,
        [FromServices] IValidator<SubmitReviewCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new SubmitReviewCommand(userId, productId, request.StoreId, request.Rating, request.Comment);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(command, cancellationToken));
    }

    [HttpGet("products/{productId:int}/reviews")]
    public async Task<IActionResult> GetForProduct(
        int productId,
        [FromServices] IQueryHandler<GetReviewsQuery, GetReviewsResult> handler,
        [FromServices] IValidator<GetReviewsQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetReviewsQuery(productId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpPost("reviews/{reviewId:int}/reply")]
    [Authorize("StorePartner")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> Reply(
        int reviewId,
        ReplyToReviewRequest request,
        [FromServices] ICommandHandler<ReplyToReviewCommand, ReplyToReviewResult> handler,
        [FromServices] IValidator<ReplyToReviewCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ReplyToReviewCommand(reviewId, userId, request.Message);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ReplyToReviewOutcome.Replied => Ok(result),
            ReplyToReviewOutcome.ReviewNotFound => NotFound(),
            ReplyToReviewOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }
}
