using System.Security.Claims;
using Application.Common;
using Application.Payments.Commands.IssueStoreCredit;
using Application.Payments.Commands.RedeemStoreCredit;
using Application.Payments.Queries.GetStoreCreditBalance;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/store-credit")]
[Authorize("StorePartner")]
public sealed class StoreCreditController : ControllerBase
{
    [HttpPost("issue")]
    public async Task<IActionResult> Issue(
        IssueStoreCreditRequest request,
        [FromServices] ICommandHandler<IssueStoreCreditCommand, IssueStoreCreditResult> handler,
        [FromServices] IValidator<IssueStoreCreditCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new IssueStoreCreditCommand(request.StoreId, request.CustomerId, request.Amount, request.Currency, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            IssueStoreCreditOutcome.Issued => Ok(result),
            IssueStoreCreditOutcome.StoreNotFound => NotFound("Store not found."),
            IssueStoreCreditOutcome.CustomerNotFound => NotFound("Customer not found."),
            IssueStoreCreditOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpPost("redeem")]
    public async Task<IActionResult> Redeem(
        RedeemStoreCreditRequest request,
        [FromServices] ICommandHandler<RedeemStoreCreditCommand, RedeemStoreCreditResult> handler,
        [FromServices] IValidator<RedeemStoreCreditCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new RedeemStoreCreditCommand(request.StoreId, request.CustomerId, request.Amount, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            RedeemStoreCreditOutcome.Redeemed => Ok(result),
            RedeemStoreCreditOutcome.StoreNotFound => NotFound("Store not found."),
            RedeemStoreCreditOutcome.Forbidden => Forbid(),
            RedeemStoreCreditOutcome.NoCreditOnFile => NotFound("No store credit on file for this customer."),
            RedeemStoreCreditOutcome.InsufficientBalance => Conflict("Insufficient store credit balance."),
            _ => Problem()
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetBalance(
        int storeId,
        int customerId,
        [FromServices] IQueryHandler<GetStoreCreditBalanceQuery, GetStoreCreditBalanceResult> handler,
        [FromServices] IValidator<GetStoreCreditBalanceQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetStoreCreditBalanceQuery(storeId, customerId, userId);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome switch
        {
            GetStoreCreditBalanceOutcome.Found => Ok(result),
            GetStoreCreditBalanceOutcome.StoreNotFound => NotFound(),
            GetStoreCreditBalanceOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }
}
