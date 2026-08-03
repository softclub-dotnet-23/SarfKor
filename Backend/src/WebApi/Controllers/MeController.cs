using System.Security.Claims;
using Application.Common;
using Application.Identity.Commands.ChangePassword;
using Application.Identity.Commands.RecordUserConsent;
using Application.Identity.Commands.UpdateUserProfile;
using Application.Identity.Queries.GetSecurityEvents;
using Application.Identity.Queries.GetUserConsents;
using Application.Identity.Queries.GetUserProfile;
using Application.Stores.Queries.GetMyStores;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

// Self-service identity: profile, consent, security event history. All three are scoped to the
// caller's own JWT-derived UserId — there is no "view someone else's" variant of any of them.
[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        UpdateUserProfileRequest request,
        [FromServices] ICommandHandler<UpdateUserProfileCommand, UpdateUserProfileResult> handler,
        [FromServices] IValidator<UpdateUserProfileCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new UpdateUserProfileCommand(userId, request.DisplayName, request.AvatarReference, request.PreferredLanguage);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(command, cancellationToken));
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(
        [FromServices] IQueryHandler<GetUserProfileQuery, GetUserProfileResult> handler,
        [FromServices] IValidator<GetUserProfileQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetUserProfileQuery(userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpPut("consents")]
    public async Task<IActionResult> RecordConsent(
        RecordUserConsentRequest request,
        [FromServices] ICommandHandler<RecordUserConsentCommand, RecordUserConsentResult> handler,
        [FromServices] IValidator<RecordUserConsentCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new RecordUserConsentCommand(userId, request.Type, request.IsGranted);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(command, cancellationToken));
    }

    [HttpGet("consents")]
    public async Task<IActionResult> GetConsents(
        [FromServices] IQueryHandler<GetUserConsentsQuery, GetUserConsentsResult> handler,
        [FromServices] IValidator<GetUserConsentsQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetUserConsentsQuery(userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpGet("security-events")]
    public async Task<IActionResult> GetSecurityEvents(
        [FromServices] IQueryHandler<GetSecurityEventsQuery, GetSecurityEventsResult> handler,
        [FromServices] IValidator<GetSecurityEventsQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetSecurityEventsQuery(userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpPut("password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        [FromServices] ICommandHandler<ChangePasswordCommand, ChangePasswordResult> handler,
        [FromServices] IValidator<ChangePasswordCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ChangePasswordOutcome.Changed => Ok(result),
            ChangePasswordOutcome.WrongCurrentPassword => BadRequest(new { outcome = "WrongCurrentPassword" }),
            ChangePasswordOutcome.UserNotFound => NotFound(),
            _ => Problem()
        };
    }

    // The backend never handed the frontend a way to recover which store(s) an owner/cashier
    // belongs to after a fresh login (see StoresController comments on employee auth) — this is
    // that missing lookup, combining owned stores and stores the caller is a registered employee of.
    [HttpGet("stores")]
    public async Task<IActionResult> GetMyStores(
        [FromServices] IQueryHandler<GetMyStoresQuery, GetMyStoresResult> handler,
        [FromServices] IValidator<GetMyStoresQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetMyStoresQuery(userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }
}
