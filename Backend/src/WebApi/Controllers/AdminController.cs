using System.Security.Claims;
using Application.Analytics.Queries.GetStoreDiagnostics;
using Application.Auditing.Queries.GetAuditLog;
using Application.Auditing.Queries.GetRecentAuditLogs;
using Application.Common;
using Application.Identity.Commands.InviteAdmin;
using Application.Stores.Commands.AdminCreateStorePartner;
using Application.Stores.Commands.ApproveStore;
using Application.Stores.Commands.ChangeStoreStatus;
using Application.Stores.Commands.UpdateStoreTaxSettings;
using Application.Stores.Queries.GetAllStores;
using Application.Stores.Queries.GetStoreDetail;
using Application.Stores.Queries.GetStoreEmployeesForAdmin;
using Application.Stores.Queries.GetStoreLocations;
using Application.Stores.Queries.GetStores;
using Domain.Stores;
using Domain.Subscriptions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize("Admin")]
[EnableRateLimiting("partner-write")]
public sealed class AdminController : ControllerBase
{
    [HttpPost("store-partners")]
    public async Task<IActionResult> AdminCreateStorePartner(
        AdminCreateStorePartnerRequest request,
        [FromServices] ICommandHandler<AdminCreateStorePartnerCommand, AdminCreateStorePartnerResult> handler,
        [FromServices] IValidator<AdminCreateStorePartnerCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new AdminCreateStorePartnerCommand(
            userId, request.Email, request.StoreName, request.Address, request.Latitude, request.Longitude,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            AdminCreateStorePartnerOutcome.Invited => Ok(result),
            AdminCreateStorePartnerOutcome.EmailAlreadyRegistered => Conflict("This email already has an account — use the existing account instead."),
            _ => Problem()
        };
    }

    [HttpPost("stores/{storeId:int}/approve")]
    public async Task<IActionResult> ApproveStore(
        int storeId,
        [FromServices] ICommandHandler<ApproveStoreCommand, ApproveStoreResult> handler,
        [FromServices] IValidator<ApproveStoreCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ApproveStoreCommand(storeId, userId, HttpContext.Connection.RemoteIpAddress?.ToString());

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ApproveStoreOutcome.Approved => Ok(result),
            ApproveStoreOutcome.NotFound => NotFound(),
            ApproveStoreOutcome.AlreadyApproved => Conflict("This store has already been approved."),
            _ => Problem()
        };
    }

    // Every administrative Store.Status transition except Approve (see ChangeStoreStatusCommand)
    // — reject/suspend/unsuspend/block/unblock/archive all go through this one endpoint, Reason
    // always required (ADMIN_PROMPT.md §2: "каждая операция, отключающая кого-либо, обязательно
    // требует причину").
    [HttpPost("stores/{storeId:int}/status")]
    public async Task<IActionResult> ChangeStoreStatus(
        int storeId,
        ChangeStoreStatusRequest request,
        [FromServices] ICommandHandler<ChangeStoreStatusCommand, ChangeStoreStatusResult> handler,
        [FromServices] IValidator<ChangeStoreStatusCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ChangeStoreStatusCommand(storeId, request.NewStatus, request.Reason, userId, HttpContext.Connection.RemoteIpAddress?.ToString());

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ChangeStoreStatusOutcome.Changed => Ok(result),
            ChangeStoreStatusOutcome.NotFound => NotFound(),
            ChangeStoreStatusOutcome.IllegalTransition => Conflict("This status transition isn't allowed from the store's current status."),
            _ => Problem()
        };
    }

    [HttpPut("stores/{storeId:int}/tax-settings")]
    public async Task<IActionResult> UpdateStoreTaxSettings(
        int storeId,
        UpdateStoreTaxSettingsRequest request,
        [FromServices] ICommandHandler<UpdateStoreTaxSettingsCommand, UpdateStoreTaxSettingsResult> handler,
        [FromServices] IValidator<UpdateStoreTaxSettingsCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new UpdateStoreTaxSettingsCommand(storeId, request.IsVatPayer, request.TaxRegime, userId, HttpContext.Connection.RemoteIpAddress?.ToString());

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            UpdateStoreTaxSettingsOutcome.Updated => Ok(result),
            UpdateStoreTaxSettingsOutcome.NotFound => NotFound(),
            _ => Problem()
        };
    }

    // Kept alongside GetStores (below) rather than replaced — Assistant's GetAllStoresTool calls
    // this simpler shape (no filters), and changing its contract isn't in scope here.
    [HttpGet("stores/all")]
    public async Task<IActionResult> GetAllStores(
        int? skip,
        int? take,
        [FromServices] IQueryHandler<GetAllStoresQuery, GetAllStoresResult> handler,
        [FromServices] IValidator<GetAllStoresQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetAllStoresQuery(skip ?? 0, take ?? 50);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpGet("stores")]
    public async Task<IActionResult> GetStores(
        int? skip,
        int? take,
        StoreStatus? status,
        SubscriptionStatus? subscriptionStatus,
        DateTimeOffset? connectedFrom,
        DateTimeOffset? connectedTo,
        string? search,
        string? sortBy,
        bool? sortDescending,
        [FromServices] IQueryHandler<GetStoresQuery, GetStoresResult> handler,
        [FromServices] IValidator<GetStoresQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetStoresQuery(
            skip ?? 0, take ?? 50, status, subscriptionStatus, connectedFrom, connectedTo, search, sortBy, sortDescending ?? false);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpGet("stores/{storeId:int}")]
    public async Task<IActionResult> GetStoreDetail(
        int storeId,
        [FromServices] IQueryHandler<GetStoreDetailQuery, GetStoreDetailResult> handler,
        [FromServices] IValidator<GetStoreDetailQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetStoreDetailQuery(storeId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome == GetStoreDetailOutcome.Found ? Ok(result) : NotFound();
    }

    [HttpGet("stores/{storeId:int}/diagnostics")]
    public async Task<IActionResult> GetStoreDiagnostics(
        int storeId,
        [FromServices] IQueryHandler<GetStoreDiagnosticsQuery, GetStoreDiagnosticsResult> handler,
        [FromServices] IValidator<GetStoreDiagnosticsQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetStoreDiagnosticsQuery(storeId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome == GetStoreDiagnosticsOutcome.Found ? Ok(result) : NotFound();
    }

    [HttpGet("stores/{storeId:int}/locations")]
    public async Task<IActionResult> GetStoreLocations(
        int storeId,
        [FromServices] IQueryHandler<GetStoreLocationsQuery, GetStoreLocationsResult> handler,
        [FromServices] IValidator<GetStoreLocationsQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetStoreLocationsQuery(storeId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome == GetStoreLocationsOutcome.Found ? Ok(result) : NotFound();
    }

    [HttpGet("stores/{storeId:int}/employees")]
    public async Task<IActionResult> GetStoreEmployeesForAdmin(
        int storeId,
        [FromServices] IQueryHandler<GetStoreEmployeesForAdminQuery, GetStoreEmployeesForAdminResult> handler,
        [FromServices] IValidator<GetStoreEmployeesForAdminQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetStoreEmployeesForAdminQuery(storeId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome == GetStoreEmployeesForAdminOutcome.Found ? Ok(result) : NotFound();
    }

    [HttpPost("invitations")]
    public async Task<IActionResult> InviteAdmin(
        InviteAdminRequest request,
        [FromServices] ICommandHandler<InviteAdminCommand, InviteAdminResult> handler,
        [FromServices] IValidator<InviteAdminCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new InviteAdminCommand(request.Email, userId, HttpContext.Connection.RemoteIpAddress?.ToString());
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            InviteAdminOutcome.Invited => Ok(result),
            InviteAdminOutcome.EmailAlreadyRegistered => Conflict("This email already has an account."),
            _ => Problem()
        };
    }

    [HttpGet("audit-logs/recent")]
    public async Task<IActionResult> GetRecentAuditLogs(
        [FromQuery] int count,
        [FromServices] IQueryHandler<GetRecentAuditLogsQuery, GetRecentAuditLogsResult> handler,
        [FromServices] IValidator<GetRecentAuditLogsQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetRecentAuditLogsQuery(count == 0 ? 20 : count);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLog(
        int? skip, int? take, string? performedByUserId, string? action, string? entityType, int? entityId,
        DateTimeOffset? from, DateTimeOffset? to,
        [FromServices] IQueryHandler<GetAuditLogQuery, GetAuditLogResult> handler,
        [FromServices] IValidator<GetAuditLogQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetAuditLogQuery(skip ?? 0, take ?? 50, performedByUserId, action, entityType, entityId, from, to);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }
}
