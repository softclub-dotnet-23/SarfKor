using System.Security.Claims;
using Application.Common;
using Application.Sales.Queries.GetCashierAnomalyReport;
using Application.Sales.Queries.GetDailySalesReport;
using Application.Sales.Queries.GetProfitReport;
using Application.Stores.Commands.CreateStore;
using Application.Stores.Commands.CreateStoreEmployeeInvitation;
using Application.Stores.Commands.RemoveStoreEmployee;
using Application.Stores.Commands.ResendStoreEmployeeInvitation;
using Application.Stores.Commands.RevokeStoreEmployeeInvitation;
using Application.Stores.Commands.UpdateStoreEmployee;
using Application.Stores.Queries.GetStoreDashboard;
using Application.Stores.Queries.GetStoreEmployeeInvitations;
using Application.Stores.Queries.GetStoreEmployees;
using Domain.Stores;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api")]
public sealed class StoresController : ControllerBase
{
    [HttpPost("stores")]
    [Authorize]
    [EnableRateLimiting("contributions")]
    public async Task<IActionResult> CreateStore(
        CreateStoreRequest request,
        [FromServices] ICommandHandler<CreateStoreCommand, CreateStoreResult> handler,
        [FromServices] IValidator<CreateStoreCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new CreateStoreCommand(userId, request.Name, request.Address, request.Latitude, request.Longitude);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("store-employees/{storeEmployeeId:int}")]
    [Authorize("StorePartner")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> RemoveEmployee(
        int storeEmployeeId,
        [FromServices] ICommandHandler<RemoveStoreEmployeeCommand, RemoveStoreEmployeeResult> handler,
        [FromServices] IValidator<RemoveStoreEmployeeCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new RemoveStoreEmployeeCommand(storeEmployeeId, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            RemoveStoreEmployeeOutcome.Removed => Ok(result),
            RemoveStoreEmployeeOutcome.NotFound => NotFound(),
            RemoveStoreEmployeeOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpPatch("store-employees/{storeEmployeeId:int}")]
    [Authorize("StorePartner")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> UpdateStoreEmployee(
        int storeEmployeeId,
        UpdateStoreEmployeeRequest request,
        [FromServices] ICommandHandler<UpdateStoreEmployeeCommand, UpdateStoreEmployeeResult> handler,
        [FromServices] IValidator<UpdateStoreEmployeeCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new UpdateStoreEmployeeCommand(
            storeEmployeeId, request.MonthlySalaryAmount, request.MonthlySalaryCurrency, request.ScheduleStart, request.ScheduleEnd, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            UpdateStoreEmployeeOutcome.Updated => Ok(result),
            UpdateStoreEmployeeOutcome.NotFound => NotFound(),
            UpdateStoreEmployeeOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpGet("stores/{storeId:int}/employees")]
    [Authorize("StorePartner")]
    public async Task<IActionResult> GetEmployees(
        int storeId,
        [FromServices] IQueryHandler<GetStoreEmployeesQuery, GetStoreEmployeesResult> handler,
        [FromServices] IValidator<GetStoreEmployeesQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetStoreEmployeesQuery(storeId, userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome switch
        {
            GetStoreEmployeesOutcome.Found => Ok(result),
            GetStoreEmployeesOutcome.StoreNotFound => NotFound(),
            GetStoreEmployeesOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    // Replaces AddEmployee's direct-attach path (kept above only for backward compatibility —
    // ADMIN_PROMPT-style task note: the frontend no longer calls it) — every new employee, existing
    // account or not, goes through an emailed link they have to click and confirm themselves.
    [HttpPost("stores/{storeId:int}/employee-invitations")]
    [Authorize("StorePartner")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> CreateEmployeeInvitation(
        int storeId,
        CreateStoreEmployeeInvitationRequest request,
        [FromServices] ICommandHandler<CreateStoreEmployeeInvitationCommand, CreateStoreEmployeeInvitationResult> handler,
        [FromServices] IValidator<CreateStoreEmployeeInvitationCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new CreateStoreEmployeeInvitationCommand(storeId, request.Email, request.Role, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            CreateStoreEmployeeInvitationOutcome.Sent => Ok(result),
            CreateStoreEmployeeInvitationOutcome.StoreNotFound => NotFound("Store not found."),
            CreateStoreEmployeeInvitationOutcome.Forbidden => Forbid(),
            CreateStoreEmployeeInvitationOutcome.AlreadyEmployed => Conflict("This user is already an employee of this store."),
            _ => Problem()
        };
    }

    [HttpGet("stores/{storeId:int}/employee-invitations")]
    [Authorize("StorePartner")]
    public async Task<IActionResult> GetEmployeeInvitations(
        int storeId,
        StoreEmployeeInvitationStatus? status,
        [FromServices] IQueryHandler<GetStoreEmployeeInvitationsQuery, GetStoreEmployeeInvitationsResult> handler,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var result = await handler.Handle(new GetStoreEmployeeInvitationsQuery(storeId, userId, status), cancellationToken);
        return result.Outcome switch
        {
            GetStoreEmployeeInvitationsOutcome.Found => Ok(result),
            GetStoreEmployeeInvitationsOutcome.StoreNotFound => NotFound(),
            GetStoreEmployeeInvitationsOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpPost("store-employee-invitations/{invitationId:int}/revoke")]
    [Authorize("StorePartner")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> RevokeEmployeeInvitation(
        int invitationId,
        [FromServices] ICommandHandler<RevokeStoreEmployeeInvitationCommand, RevokeStoreEmployeeInvitationResult> handler,
        [FromServices] IValidator<RevokeStoreEmployeeInvitationCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new RevokeStoreEmployeeInvitationCommand(invitationId, userId);
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            RevokeStoreEmployeeInvitationOutcome.Revoked => Ok(result),
            RevokeStoreEmployeeInvitationOutcome.NotFound => NotFound(),
            RevokeStoreEmployeeInvitationOutcome.Forbidden => Forbid(),
            RevokeStoreEmployeeInvitationOutcome.NotPending => Conflict("This invitation is no longer pending."),
            _ => Problem()
        };
    }

    // Its own rate-limit bucket, not "partner-write" — this is the one owner action here that
    // sends an email, and task spec explicitly calls out "с ограничением частоты".
    [HttpPost("store-employee-invitations/{invitationId:int}/resend")]
    [Authorize("StorePartner")]
    [EnableRateLimiting("invite-resend")]
    public async Task<IActionResult> ResendEmployeeInvitation(
        int invitationId,
        [FromServices] ICommandHandler<ResendStoreEmployeeInvitationCommand, ResendStoreEmployeeInvitationResult> handler,
        [FromServices] IValidator<ResendStoreEmployeeInvitationCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ResendStoreEmployeeInvitationCommand(invitationId, userId);
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ResendStoreEmployeeInvitationOutcome.Resent => Ok(result),
            ResendStoreEmployeeInvitationOutcome.NotFound => NotFound(),
            ResendStoreEmployeeInvitationOutcome.Forbidden => Forbid(),
            ResendStoreEmployeeInvitationOutcome.NotPending => Conflict("This invitation is no longer pending."),
            _ => Problem()
        };
    }

    [HttpGet("stores/{storeId:int}/dashboard")]
    [Authorize("StorePartner")]
    public async Task<IActionResult> GetDashboard(
        int storeId,
        [FromServices] IQueryHandler<GetStoreDashboardQuery, GetStoreDashboardResult> handler,
        [FromServices] IValidator<GetStoreDashboardQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetStoreDashboardQuery(storeId, userId);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome switch
        {
            GetStoreDashboardOutcome.Found => Ok(result),
            GetStoreDashboardOutcome.StoreNotFound => NotFound("Store not found."),
            GetStoreDashboardOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpGet("stores/{storeId:int}/reports/daily-sales")]
    [Authorize("StorePartner")]
    public async Task<IActionResult> GetDailySalesReport(
        int storeId,
        DateOnly date,
        [FromServices] IQueryHandler<GetDailySalesReportQuery, GetDailySalesReportResult> handler,
        [FromServices] IValidator<GetDailySalesReportQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetDailySalesReportQuery(storeId, date, userId);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome switch
        {
            GetDailySalesReportOutcome.Found => Ok(result),
            GetDailySalesReportOutcome.StoreNotFound => NotFound("Store not found."),
            GetDailySalesReportOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpGet("stores/{storeId:int}/reports/profit")]
    [Authorize("StorePartner")]
    public async Task<IActionResult> GetProfitReport(
        int storeId,
        DateOnly from,
        DateOnly to,
        [FromServices] IQueryHandler<GetProfitReportQuery, GetProfitReportResult> handler,
        [FromServices] IValidator<GetProfitReportQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetProfitReportQuery(storeId, from, to, userId);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome switch
        {
            GetProfitReportOutcome.Found => Ok(result),
            GetProfitReportOutcome.StoreNotFound => NotFound("Store not found."),
            GetProfitReportOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpGet("stores/{storeId:int}/reports/cashier-anomalies")]
    [Authorize("StorePartner")]
    public async Task<IActionResult> GetCashierAnomalyReport(
        int storeId,
        DateOnly from,
        DateOnly to,
        [FromServices] IQueryHandler<GetCashierAnomalyReportQuery, GetCashierAnomalyReportResult> handler,
        [FromServices] IValidator<GetCashierAnomalyReportQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetCashierAnomalyReportQuery(storeId, from, to, userId);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome switch
        {
            GetCashierAnomalyReportOutcome.Found => Ok(result),
            GetCashierAnomalyReportOutcome.StoreNotFound => NotFound("Store not found."),
            GetCashierAnomalyReportOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }
}
