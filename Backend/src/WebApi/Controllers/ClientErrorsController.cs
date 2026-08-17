using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

/// <summary>
/// A render crash the frontend's RouteErrorBoundary catches happens only in a user's own browser
/// — with no monitoring dashboard (CLAUDE.md §10), that exception is otherwise invisible to us
/// unless the user happens to describe it. This is the "и в лог" half of "показывай понятную
/// ошибку, а технические детали пиши в консоль и в лог": the user-facing screen stays a clean,
/// specific message (ErrorState), while this endpoint gives us the same exception server-side
/// where it actually gets seen (Railway's log stream).
///
/// Deliberately unauthenticated — a crash can happen before login succeeds or after a token has
/// expired, and this must never itself throw. Rate-limited by IP (not by user, for the same
/// reason) and every field is length-capped before it touches the log, since this is the one
/// endpoint on the platform that accepts arbitrary free text from a client with no schema behind
/// it (a real exception message/stack), so it must not become a log-flooding or storage vector.
/// </summary>
[ApiController]
[Route("api/client-errors")]
[AllowAnonymous]
public sealed class ClientErrorsController(ILogger<ClientErrorsController> logger) : ControllerBase
{
    private const int MaxFieldLength = 2000;

    public sealed record ReportClientErrorRequest(string Message, string? Stack, string? Section, string? Url);

    [HttpPost]
    [EnableRateLimiting("client-error-report")]
    public IActionResult Report([FromBody] ReportClientErrorRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest();

        // Never trust client-supplied text at unbounded length -- this is a public, unauthenticated
        // sink by design (see class remarks), so it gets the same treatment untrusted upload input
        // would: hard-capped before it's ever written anywhere.
        static string? Cap(string? s) => string.IsNullOrEmpty(s) ? s : s[..Math.Min(s.Length, MaxFieldLength)];

        var userId = User.Identity?.IsAuthenticated == true
            ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : null;

        logger.LogError(
            "Client render crash in section {Section} at {Url} (user {UserId}): {Message}\n{Stack}",
            Cap(request.Section) ?? "(unknown)",
            Cap(request.Url) ?? "(unknown)",
            userId ?? "(anonymous)",
            Cap(request.Message),
            Cap(request.Stack) ?? "(no stack)");

        return NoContent();
    }
}
