using System.Security.Claims;
using Application.Common;
using Application.Sales.Commands.CloseCashierShift;
using Application.Sales.Commands.OpenCashierShift;
using Application.Sales.Queries.GetCashierShifts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

// Cashier shifts. No separate "cashier" sub-role exists yet (CLAUDE.md §9 open question) — for now
// the store owner opens/closes their own shifts.
[ApiController]
[Route("api")]
[Authorize("StorePartner")]
public sealed class CashierShiftsController : ControllerBase
{
    [HttpPost("cashier-shifts/open")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> Open(
        OpenCashierShiftRequest request,
        [FromServices] ICommandHandler<OpenCashierShiftCommand, OpenCashierShiftResult> handler,
        [FromServices] IValidator<OpenCashierShiftCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new OpenCashierShiftCommand(request.StoreId, request.OpeningCash, request.Currency, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            OpenCashierShiftOutcome.Opened => Ok(result),
            OpenCashierShiftOutcome.StoreNotFound => NotFound("Store not found."),
            OpenCashierShiftOutcome.Forbidden => Forbid(),
            OpenCashierShiftOutcome.SubscriptionInactive => StatusCode(402, "Subscription is not active — the register is closed until the store's subscription is current."),
            _ => Problem()
        };
    }

    [HttpPost("cashier-shifts/{cashierShiftId:int}/close")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> Close(
        int cashierShiftId,
        CloseCashierShiftRequest request,
        [FromServices] ICommandHandler<CloseCashierShiftCommand, CloseCashierShiftResult> handler,
        [FromServices] IValidator<CloseCashierShiftCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new CloseCashierShiftCommand(cashierShiftId, request.ClosingCash, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            CloseCashierShiftOutcome.Closed => Ok(result),
            CloseCashierShiftOutcome.NotFound => NotFound(),
            CloseCashierShiftOutcome.Forbidden => Forbid(),
            CloseCashierShiftOutcome.AlreadyClosed => Conflict("This shift has already been closed."),
            CloseCashierShiftOutcome.SubscriptionInactive => StatusCode(402, "Subscription is not active — the register is closed until the store's subscription is current."),
            _ => Problem()
        };
    }

    [HttpGet("stores/{storeId:int}/cashier-shifts")]
    public async Task<IActionResult> GetForStore(
        int storeId,
        [FromServices] IQueryHandler<GetCashierShiftsQuery, GetCashierShiftsResult> handler,
        [FromServices] IValidator<GetCashierShiftsQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetCashierShiftsQuery(storeId, userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome switch
        {
            GetCashierShiftsOutcome.Found => Ok(result),
            GetCashierShiftsOutcome.StoreNotFound => NotFound(),
            GetCashierShiftsOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }
}
