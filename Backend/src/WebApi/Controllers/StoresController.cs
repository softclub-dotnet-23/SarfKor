using System.Security.Claims;
using Application.Common;
using Application.Sales.Queries.GetCashierAnomalyReport;
using Application.Sales.Queries.GetDailySalesReport;
using Application.Sales.Queries.GetProfitReport;
using Application.Stores.Commands.AddStoreEmployee;
using Application.Stores.Commands.CreateStore;
using Application.Stores.Commands.RemoveStoreEmployee;
using Application.Stores.Queries.GetStoreDashboard;
using Application.Stores.Queries.GetStoreEmployees;
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

    [HttpPost("stores/{storeId:int}/employees")]
    [Authorize("StorePartner")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> AddEmployee(
        int storeId,
        AddStoreEmployeeRequest request,
        [FromServices] ICommandHandler<AddStoreEmployeeCommand, AddStoreEmployeeResult> handler,
        [FromServices] IValidator<AddStoreEmployeeCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new AddStoreEmployeeCommand(storeId, request.EmployeeEmail, request.Role, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            AddStoreEmployeeOutcome.Added => Ok(result),
            AddStoreEmployeeOutcome.StoreNotFound => NotFound("Store not found."),
            AddStoreEmployeeOutcome.Forbidden => Forbid(),
            AddStoreEmployeeOutcome.AlreadyEmployed => Conflict("This user is already an employee of this store."),
            AddStoreEmployeeOutcome.Invited => Ok(result),
            _ => Problem()
        };
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
