using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

// Foundation for external monitoring (CLAUDE.md §10: "нужен дашборд состояния системы") — an
// uptime check or Grafana/Prometheus scraper polls this; deliberately unauthenticated (that's
// standard for health probes) and returns only a status/timestamp, never internal details.
[ApiController]
[Route("health")]
public sealed class HealthController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow })
                : StatusCode(503, new { status = "unhealthy", timestamp = DateTimeOffset.UtcNow });
        }
        catch
        {
            return StatusCode(503, new { status = "unhealthy", timestamp = DateTimeOffset.UtcNow });
        }
    }
}
