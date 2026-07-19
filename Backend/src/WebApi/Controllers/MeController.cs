using System.Security.Claims;
using Application.Common;
using Application.Identity.Commands.RecordUserConsent;
using Application.Identity.Commands.UpdateUserProfile;
using Application.Identity.Queries.GetSecurityEvents;
using Application.Identity.Queries.GetUserConsents;
using Application.Identity.Queries.GetUserProfile;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}
