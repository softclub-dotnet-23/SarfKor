using System.Security.Claims;
using Application.Common;
using Application.Loyalty.Commands.CreateLoyaltyProgram;
using Application.Loyalty.Commands.EarnLoyaltyPoints;
using Application.Loyalty.Commands.EnrollCustomerInLoyalty;
using Application.Loyalty.Commands.RedeemLoyaltyPoints;
using Application.Loyalty.Queries.GetLoyaltyAccount;
using Application.Loyalty.Queries.GetLoyaltyProgram;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api")]
[Authorize("StorePartner")]
public sealed class LoyaltyController : ControllerBase
{
    [HttpPost("loyalty-programs")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> CreateProgram(
        CreateLoyaltyProgramRequest request,
        [FromServices] ICommandHandler<CreateLoyaltyProgramCommand, CreateLoyaltyProgramResult> handler,
        [FromServices] IValidator<CreateLoyaltyProgramCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new CreateLoyaltyProgramCommand(request.StoreId, request.PointsPerCurrencyUnit, request.RedemptionRate, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            CreateLoyaltyProgramOutcome.Created => Ok(result),
            CreateLoyaltyProgramOutcome.StoreNotFound => NotFound("Store not found."),
            CreateLoyaltyProgramOutcome.Forbidden => Forbid(),
            CreateLoyaltyProgramOutcome.AlreadyExists => Conflict("This store already has a loyalty program."),
            CreateLoyaltyProgramOutcome.SubscriptionInactive => StatusCode(402, "Subscription is not active — the cabinet is closed until the store's subscription is current."),
            _ => Problem()
        };
    }

    [HttpGet("stores/{storeId:int}/loyalty-program")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProgram(
        int storeId,
        [FromServices] IQueryHandler<GetLoyaltyProgramQuery, GetLoyaltyProgramResult> handler,
        [FromServices] IValidator<GetLoyaltyProgramQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetLoyaltyProgramQuery(storeId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpPost("loyalty-accounts/enroll")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> Enroll(
        EnrollCustomerInLoyaltyRequest request,
        [FromServices] ICommandHandler<EnrollCustomerInLoyaltyCommand, EnrollCustomerInLoyaltyResult> handler,
        [FromServices] IValidator<EnrollCustomerInLoyaltyCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new EnrollCustomerInLoyaltyCommand(request.CustomerId, request.LoyaltyProgramId, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            EnrollCustomerInLoyaltyOutcome.Enrolled => Ok(result),
            EnrollCustomerInLoyaltyOutcome.AlreadyEnrolled => Ok(result),
            EnrollCustomerInLoyaltyOutcome.CustomerNotFound => NotFound("Customer not found."),
            EnrollCustomerInLoyaltyOutcome.ProgramNotFound => NotFound("Loyalty program not found."),
            EnrollCustomerInLoyaltyOutcome.Forbidden => Forbid(),
            EnrollCustomerInLoyaltyOutcome.SubscriptionInactive => StatusCode(402, "Subscription is not active — the cabinet is closed until the store's subscription is current."),
            _ => Problem()
        };
    }

    [HttpPost("loyalty-accounts/{loyaltyAccountId:int}/earn")]
    [EnableRateLimiting("money-write")]
    public async Task<IActionResult> Earn(
        int loyaltyAccountId,
        EarnLoyaltyPointsRequest request,
        [FromServices] ICommandHandler<EarnLoyaltyPointsCommand, EarnLoyaltyPointsResult> handler,
        [FromServices] IValidator<EarnLoyaltyPointsCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new EarnLoyaltyPointsCommand(loyaltyAccountId, request.Points, request.SaleTransactionId, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            EarnLoyaltyPointsOutcome.Earned => Ok(result),
            EarnLoyaltyPointsOutcome.AccountNotFound => NotFound(),
            EarnLoyaltyPointsOutcome.Forbidden => Forbid(),
            EarnLoyaltyPointsOutcome.SubscriptionInactive => StatusCode(402, "Subscription is not active — the cabinet is closed until the store's subscription is current."),
            _ => Problem()
        };
    }

    [HttpPost("loyalty-accounts/{loyaltyAccountId:int}/redeem")]
    [EnableRateLimiting("money-write")]
    public async Task<IActionResult> Redeem(
        int loyaltyAccountId,
        RedeemLoyaltyPointsRequest request,
        [FromServices] ICommandHandler<RedeemLoyaltyPointsCommand, RedeemLoyaltyPointsResult> handler,
        [FromServices] IValidator<RedeemLoyaltyPointsCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new RedeemLoyaltyPointsCommand(loyaltyAccountId, request.Points, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            RedeemLoyaltyPointsOutcome.Redeemed => Ok(result),
            RedeemLoyaltyPointsOutcome.AccountNotFound => NotFound(),
            RedeemLoyaltyPointsOutcome.Forbidden => Forbid(),
            RedeemLoyaltyPointsOutcome.InsufficientPoints => Conflict("Insufficient points balance."),
            RedeemLoyaltyPointsOutcome.SubscriptionInactive => StatusCode(402, "Subscription is not active — the cabinet is closed until the store's subscription is current."),
            _ => Problem()
        };
    }

    [HttpGet("loyalty-accounts")]
    public async Task<IActionResult> GetAccount(
        int customerId,
        int loyaltyProgramId,
        [FromServices] IQueryHandler<GetLoyaltyAccountQuery, GetLoyaltyAccountResult> handler,
        [FromServices] IValidator<GetLoyaltyAccountQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetLoyaltyAccountQuery(customerId, loyaltyProgramId, userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome switch
        {
            GetLoyaltyAccountOutcome.Found => Ok(result),
            GetLoyaltyAccountOutcome.NotFound => NotFound(),
            GetLoyaltyAccountOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }
}
