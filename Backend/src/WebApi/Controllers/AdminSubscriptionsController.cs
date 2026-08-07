using System.Security.Claims;
using Application.Common;
using Application.Subscriptions.Commands.CancelStoreSubscription;
using Application.Subscriptions.Commands.ChangeStoreSubscriptionPlan;
using Application.Subscriptions.Commands.CreateSubscriptionPlan;
using Application.Subscriptions.Commands.RecordSubscriptionPayment;
using Application.Subscriptions.Commands.ReverseSubscriptionPayment;
using Application.Subscriptions.Commands.UpdateSubscriptionPlan;
using Application.Subscriptions.Queries.GetExpiringSoonSubscriptions;
using Application.Subscriptions.Queries.GetPastDueSubscriptions;
using Application.Subscriptions.Queries.GetStoreSubscriptions;
using Application.Subscriptions.Queries.GetSubscriptionPayments;
using Application.Subscriptions.Queries.GetSubscriptionPlans;
using Domain.Subscriptions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api/admin/subscriptions")]
[Authorize("Admin")]
[EnableRateLimiting("partner-write")]
public sealed class AdminSubscriptionsController : ControllerBase
{
    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan(
        CreateSubscriptionPlanRequest request,
        [FromServices] ICommandHandler<CreateSubscriptionPlanCommand, CreateSubscriptionPlanResult> handler,
        [FromServices] IValidator<CreateSubscriptionPlanCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new CreateSubscriptionPlanCommand(
            request.Name, request.Code, request.MonthlyPriceAmount, request.MonthlyPriceCurrency,
            request.MaxStores, request.MaxEmployees, request.Features, userId, HttpContext.Connection.RemoteIpAddress?.ToString());

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            CreateSubscriptionPlanOutcome.Created => Ok(result),
            CreateSubscriptionPlanOutcome.CodeAlreadyExists => Conflict("A plan with this code already exists."),
            _ => Problem()
        };
    }

    [HttpPut("plans/{subscriptionPlanId:int}")]
    public async Task<IActionResult> UpdatePlan(
        int subscriptionPlanId,
        UpdateSubscriptionPlanRequest request,
        [FromServices] ICommandHandler<UpdateSubscriptionPlanCommand, UpdateSubscriptionPlanResult> handler,
        [FromServices] IValidator<UpdateSubscriptionPlanCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new UpdateSubscriptionPlanCommand(
            subscriptionPlanId, request.Name, request.MonthlyPriceAmount, request.MonthlyPriceCurrency,
            request.MaxStores, request.MaxEmployees, request.Features, request.IsActive, userId, HttpContext.Connection.RemoteIpAddress?.ToString());

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            UpdateSubscriptionPlanOutcome.Updated => Ok(result),
            UpdateSubscriptionPlanOutcome.NotFound => NotFound(),
            _ => Problem()
        };
    }

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(
        bool? includeInactive,
        [FromServices] IQueryHandler<GetSubscriptionPlansQuery, GetSubscriptionPlansResult> handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.Handle(new GetSubscriptionPlansQuery(includeInactive ?? false), cancellationToken));

    [HttpGet("")]
    public async Task<IActionResult> GetStoreSubscriptions(
        int? skip, int? take, SubscriptionStatus? status, int? subscriptionPlanId, string? storeSearch,
        [FromServices] IQueryHandler<GetStoreSubscriptionsQuery, GetStoreSubscriptionsResult> handler,
        [FromServices] IValidator<GetStoreSubscriptionsQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetStoreSubscriptionsQuery(skip ?? 0, take ?? 50, status, subscriptionPlanId, storeSearch);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpGet("expiring-soon")]
    public async Task<IActionResult> GetExpiringSoon(
        int? withinDays,
        [FromServices] IQueryHandler<GetExpiringSoonSubscriptionsQuery, GetExpiringSoonSubscriptionsResult> handler,
        [FromServices] IValidator<GetExpiringSoonSubscriptionsQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetExpiringSoonSubscriptionsQuery(withinDays ?? 7);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpGet("past-due")]
    public async Task<IActionResult> GetPastDue(
        [FromServices] IQueryHandler<GetPastDueSubscriptionsQuery, GetPastDueSubscriptionsResult> handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.Handle(new GetPastDueSubscriptionsQuery(), cancellationToken));

    [HttpPost("{storeSubscriptionId:int}/plan")]
    public async Task<IActionResult> ChangePlan(
        int storeSubscriptionId,
        ChangeStoreSubscriptionPlanRequest request,
        [FromServices] ICommandHandler<ChangeStoreSubscriptionPlanCommand, ChangeStoreSubscriptionPlanResult> handler,
        [FromServices] IValidator<ChangeStoreSubscriptionPlanCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ChangeStoreSubscriptionPlanCommand(storeSubscriptionId, request.NewSubscriptionPlanId, userId, HttpContext.Connection.RemoteIpAddress?.ToString());

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ChangeStoreSubscriptionPlanOutcome.Changed => Ok(result),
            ChangeStoreSubscriptionPlanOutcome.SubscriptionNotFound => NotFound("Subscription not found."),
            ChangeStoreSubscriptionPlanOutcome.PlanNotFound => NotFound("Plan not found."),
            _ => Problem()
        };
    }

    [HttpPost("{storeSubscriptionId:int}/cancel")]
    public async Task<IActionResult> Cancel(
        int storeSubscriptionId,
        CancelStoreSubscriptionRequest request,
        [FromServices] ICommandHandler<CancelStoreSubscriptionCommand, CancelStoreSubscriptionResult> handler,
        [FromServices] IValidator<CancelStoreSubscriptionCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new CancelStoreSubscriptionCommand(storeSubscriptionId, request.Reason, userId, HttpContext.Connection.RemoteIpAddress?.ToString());

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            CancelStoreSubscriptionOutcome.Cancelled => Ok(result),
            CancelStoreSubscriptionOutcome.NotFound => NotFound(),
            CancelStoreSubscriptionOutcome.AlreadyCancelled => Conflict("This subscription is already cancelled."),
            _ => Problem()
        };
    }

    [HttpPost("{storeSubscriptionId:int}/payments")]
    public async Task<IActionResult> RecordPayment(
        int storeSubscriptionId,
        RecordSubscriptionPaymentRequest request,
        [FromServices] ICommandHandler<RecordSubscriptionPaymentCommand, RecordSubscriptionPaymentResult> handler,
        [FromServices] IValidator<RecordSubscriptionPaymentCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new RecordSubscriptionPaymentCommand(
            storeSubscriptionId, request.Amount, request.Currency, request.PeriodStart, request.PeriodEnd,
            request.Method, request.Comment, userId, HttpContext.Connection.RemoteIpAddress?.ToString());

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            RecordSubscriptionPaymentOutcome.Recorded => Ok(result),
            RecordSubscriptionPaymentOutcome.SubscriptionNotFound => NotFound(),
            _ => Problem()
        };
    }

    [HttpPost("payments/{subscriptionPaymentId:int}/reverse")]
    public async Task<IActionResult> ReversePayment(
        int subscriptionPaymentId,
        ReverseSubscriptionPaymentRequest request,
        [FromServices] ICommandHandler<ReverseSubscriptionPaymentCommand, ReverseSubscriptionPaymentResult> handler,
        [FromServices] IValidator<ReverseSubscriptionPaymentCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ReverseSubscriptionPaymentCommand(subscriptionPaymentId, request.Reason, userId, HttpContext.Connection.RemoteIpAddress?.ToString());

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ReverseSubscriptionPaymentOutcome.Reversed => Ok(result),
            ReverseSubscriptionPaymentOutcome.NotFound => NotFound(),
            ReverseSubscriptionPaymentOutcome.AlreadyReversed => Conflict("This payment has already been reversed."),
            _ => Problem()
        };
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(
        int? skip, int? take, int? storeId, DateOnly? from, DateOnly? to,
        [FromServices] IQueryHandler<GetSubscriptionPaymentsQuery, GetSubscriptionPaymentsResult> handler,
        [FromServices] IValidator<GetSubscriptionPaymentsQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetSubscriptionPaymentsQuery(skip ?? 0, take ?? 50, storeId, from, to);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }
}
