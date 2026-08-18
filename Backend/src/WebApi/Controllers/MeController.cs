using System.Security.Claims;
using Application.Common;
using Application.Identity.Commands.ChangePassword;
using Application.Identity.Commands.RecordUserConsent;
using Application.Identity.Commands.UpdateUserAvatar;
using Application.Identity.Commands.UpdateUserProfile;
using Application.Identity.Queries.GetSecurityEvents;
using Application.Identity.Queries.GetUserConsents;
using Application.Identity.Queries.GetUserProfile;
using Application.Stores.Queries.GetMyStores;
using Application.Stores.Queries.SearchMyStores;
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

    [HttpPost("password")]
    [EnableRateLimiting("account-security")]
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
            ChangePasswordOutcome.Succeeded => Ok(),
            ChangePasswordOutcome.IncorrectCurrentPassword => BadRequest("Текущий пароль неверен."),
            ChangePasswordOutcome.WeakPassword => BadRequest(string.Join(" ", result.Errors)),
            ChangePasswordOutcome.NotFound => NotFound(),
            _ => Problem()
        };
    }

    // Content-sniffed (magic bytes, never the client-supplied Content-Type/filename) the same way
    // ReceiptsController.Upload already does, stored under App_Data (outside wwwroot, never directly
    // web-servable) with a server-generated filename — GetAvatar below is the only way back to the
    // bytes, and it always resolves the filename from the caller's own stored profile, never from
    // client input, so there is no path-traversal surface.
    [HttpPost("avatar")]
    [EnableRateLimiting("account-security")]
    public async Task<IActionResult> UploadAvatar(
        IFormFile file,
        [FromServices] IWebHostEnvironment env,
        [FromServices] IConfiguration configuration,
        [FromServices] ICommandHandler<UpdateUserAvatarCommand, UpdateUserAvatarResult> handler,
        [FromServices] IValidator<UpdateUserAvatarCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        const long maxFileSizeBytes = 2 * 1024 * 1024;
        if (file is null || file.Length == 0 || file.Length > maxFileSizeBytes)
            return BadRequest("File is empty or exceeds the 2 MB limit.");

        var extension = await ImageContentTypeDetector.DetectExtensionAsync(file, cancellationToken);
        if (extension is null)
            return BadRequest("Unsupported file type. Only JPEG and PNG images are accepted.");

        var storageRoot = configuration["Storage:AvatarsPath"] ?? Path.Combine(env.ContentRootPath, "App_Data", "avatars");
        Directory.CreateDirectory(storageRoot);

        var storedFileName = $"{Guid.NewGuid()}{extension}";
        await using (var destination = System.IO.File.Create(Path.Combine(storageRoot, storedFileName)))
        {
            await file.CopyToAsync(destination, cancellationToken);
        }

        var command = new UpdateUserAvatarCommand(userId, storedFileName);
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            System.IO.File.Delete(Path.Combine(storageRoot, storedFileName));
            return this.ToValidationProblem(validationResult);
        }

        var result = await handler.Handle(command, cancellationToken);

        // Only delete the previous file once the new reference is safely committed.
        if (!string.IsNullOrEmpty(result.PreviousAvatarReference))
        {
            var previousPath = Path.Combine(storageRoot, result.PreviousAvatarReference);
            if (System.IO.File.Exists(previousPath))
                System.IO.File.Delete(previousPath);
        }

        return Ok(new { avatarReference = storedFileName });
    }

    [HttpGet("avatar")]
    public async Task<IActionResult> GetAvatar(
        [FromServices] IWebHostEnvironment env,
        [FromServices] IConfiguration configuration,
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

        var profile = await handler.Handle(query, cancellationToken);
        if (!profile.Found || string.IsNullOrEmpty(profile.AvatarReference))
            return NotFound();

        var storageRoot = configuration["Storage:AvatarsPath"] ?? Path.Combine(env.ContentRootPath, "App_Data", "avatars");
        var path = Path.Combine(storageRoot, profile.AvatarReference);
        if (!System.IO.File.Exists(path))
            return NotFound();

        var contentType = Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
        return PhysicalFile(path, contentType);
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

    // Owner-only (not GetMyStores' owned+employed union -- see handler remarks), searchable,
    // paginated -- backs StorePicker.tsx. This is the endpoint whose absence previously meant
    // SupplyPage's stock-transfer form asked the owner to type a destination store id by hand.
    [HttpGet("stores/search")]
    public async Task<IActionResult> SearchMyStores(
        [FromQuery] string? search,
        [FromQuery] int skip,
        [FromQuery] int take,
        [FromServices] IQueryHandler<SearchMyStoresQuery, SearchMyStoresResult> handler,
        [FromServices] IValidator<SearchMyStoresQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new SearchMyStoresQuery(userId, search, skip, take == 0 ? 20 : take);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }
}
