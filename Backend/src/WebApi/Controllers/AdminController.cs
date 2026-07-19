using System.Security.Claims;
using Application.Common;
using Application.Feedback.Commands.ModerateReport;
using Application.Feedback.Commands.ResolveReportDispute;
using Application.Feedback.Queries.GetPendingReportDisputes;
using Application.Pricing.Commands.ResolvePriceEntryDispute;
using Application.Pricing.Queries.GetPendingPriceEntryDisputes;
using Application.Products.Commands.ModerateNewProduct;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize("Admin")]
public sealed class AdminController : ControllerBase
{
    [HttpPost("price-entry-disputes/{disputeId:int}/resolve")]
    public async Task<IActionResult> ResolvePriceEntryDispute(
        int disputeId,
        ResolveDisputeRequest request,
        [FromServices] ICommandHandler<ResolvePriceEntryDisputeCommand, ResolvePriceEntryDisputeResult> handler,
        [FromServices] IValidator<ResolvePriceEntryDisputeCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ResolvePriceEntryDisputeCommand(disputeId, request.Uphold, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ResolvePriceEntryDisputeOutcome.Upheld => Ok(result),
            ResolvePriceEntryDisputeOutcome.Dismissed => Ok(result),
            ResolvePriceEntryDisputeOutcome.NotFound => NotFound(),
            ResolvePriceEntryDisputeOutcome.AlreadyResolved => Conflict("This dispute has already been resolved."),
            _ => Problem()
        };
    }

    [HttpGet("price-entry-disputes/pending")]
    public async Task<IActionResult> GetPendingPriceEntryDisputes(
        [FromServices] IQueryHandler<GetPendingPriceEntryDisputesQuery, GetPendingPriceEntryDisputesResult> handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.Handle(new GetPendingPriceEntryDisputesQuery(), cancellationToken));

    [HttpPost("report-disputes/{disputeId:int}/resolve")]
    public async Task<IActionResult> ResolveReportDispute(
        int disputeId,
        ResolveDisputeRequest request,
        [FromServices] ICommandHandler<ResolveReportDisputeCommand, ResolveReportDisputeResult> handler,
        [FromServices] IValidator<ResolveReportDisputeCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ResolveReportDisputeCommand(disputeId, request.Uphold, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ResolveReportDisputeOutcome.Upheld => Ok(result),
            ResolveReportDisputeOutcome.Dismissed => Ok(result),
            ResolveReportDisputeOutcome.NotFound => NotFound(),
            ResolveReportDisputeOutcome.AlreadyResolved => Conflict("This dispute has already been resolved."),
            _ => Problem()
        };
    }

    [HttpGet("report-disputes/pending")]
    public async Task<IActionResult> GetPendingReportDisputes(
        [FromServices] IQueryHandler<GetPendingReportDisputesQuery, GetPendingReportDisputesResult> handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.Handle(new GetPendingReportDisputesQuery(), cancellationToken));

    [HttpPost("products/{submissionId:int}/moderate")]
    public async Task<IActionResult> ModerateNewProduct(
        int submissionId,
        ModerateNewProductRequest request,
        [FromServices] ICommandHandler<ModerateNewProductCommand, ModerateNewProductResult> handler,
        [FromServices] IValidator<ModerateNewProductCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ModerateNewProductCommand(submissionId, request.Approve, userId, request.Reason);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ModerateNewProductOutcome.Approved => Ok(result),
            ModerateNewProductOutcome.Rejected => Ok(result),
            ModerateNewProductOutcome.NotFound => NotFound(),
            ModerateNewProductOutcome.AlreadyModerated => Conflict("This submission has already been moderated."),
            _ => Problem()
        };
    }

    [HttpPost("reports/{reportId:int}/moderate")]
    public async Task<IActionResult> ModerateReport(
        int reportId,
        ModerateReportRequest request,
        [FromServices] ICommandHandler<ModerateReportCommand, ModerateReportResult> handler,
        [FromServices] IValidator<ModerateReportCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ModerateReportCommand(reportId, request.Resolve, userId, request.Reason);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ModerateReportOutcome.Resolved => Ok(result),
            ModerateReportOutcome.Rejected => Ok(result),
            ModerateReportOutcome.NotFound => NotFound(),
            ModerateReportOutcome.AlreadyModerated => Conflict("This report has already been moderated."),
            _ => Problem()
        };
    }
}
