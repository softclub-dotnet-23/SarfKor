using System.Security.Claims;
using System.Text.Json;
using Application.Common;
using Application.Receipts.Commands.UploadReceipt;
using Application.Receipts.Commands.VerifyReceipt;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api/receipts")]
[Authorize]
public sealed class ReceiptsController : ControllerBase
{
    [HttpPost("{id:int}/verify")]
    [EnableRateLimiting("contributions")]
    public async Task<IActionResult> Verify(
        int id,
        [FromServices] ICommandHandler<VerifyReceiptCommand, VerifyReceiptResult> handler,
        [FromServices] IValidator<VerifyReceiptCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new VerifyReceiptCommand(id, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            VerifyReceiptOutcome.Verified => Ok(result),
            VerifyReceiptOutcome.Mismatched => Ok(result),
            VerifyReceiptOutcome.NotFound => NotFound(),
            VerifyReceiptOutcome.Forbidden => Forbid(),
            VerifyReceiptOutcome.MissingStore => Conflict("Receipt has no associated store."),
            VerifyReceiptOutcome.AlreadyProcessed => Conflict("Receipt has already been processed."),
            _ => Problem()
        };
    }

    // This API is JWT Bearer, not cookie sessions — per CLAUDE.md §2, CSRF for JWT is mitigated by the
    // strict CORS origin whitelist, not antiforgery tokens. No antiforgery middleware is registered,
    // so no explicit opt-out is needed here (unlike the old Minimal API's .DisableAntiforgery()).
    [HttpPost("upload")]
    [EnableRateLimiting("contributions")]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] int? storeId,
        [FromForm] string linesJson,
        [FromServices] IWebHostEnvironment env,
        [FromServices] IConfiguration configuration,
        [FromServices] ICommandHandler<UploadReceiptCommand, UploadReceiptResult> handler,
        [FromServices] IValidator<UploadReceiptCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        const long maxFileSizeBytes = 5 * 1024 * 1024;
        if (file.Length == 0 || file.Length > maxFileSizeBytes)
            return BadRequest("File is empty or exceeds the 5 MB limit.");

        // Content-based check (magic bytes), never trust the client-supplied Content-Type header.
        var extension = await ImageContentTypeDetector.DetectExtensionAsync(file, cancellationToken);
        if (extension is null)
            return BadRequest("Unsupported file type. Only JPEG and PNG images are accepted.");

        List<UploadReceiptLineInput>? lines;
        try
        {
            lines = JsonSerializer.Deserialize<List<UploadReceiptLineInput>>(
                linesJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return BadRequest("Invalid line items payload.");
        }

        if (lines is null)
            return BadRequest("Invalid line items payload.");

        // Stored under the content root, never under wwwroot, so it is never directly web-servable.
        var storageRoot = configuration["Storage:ReceiptsPath"] ?? Path.Combine(env.ContentRootPath, "App_Data", "receipts");
        Directory.CreateDirectory(storageRoot);

        // Filename is server-generated (never the client-supplied one) to rule out path traversal / overwrite attacks.
        var storedFileName = $"{Guid.NewGuid()}{extension}";
        await using (var destination = System.IO.File.Create(Path.Combine(storageRoot, storedFileName)))
        {
            await file.CopyToAsync(destination, cancellationToken);
        }

        var command = new UploadReceiptCommand(userId, storeId, storedFileName, lines);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return Ok(result);
    }
}
