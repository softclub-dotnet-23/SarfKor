using Application.Analytics.Queries.GetMetricsTimeSeries;
using Application.Analytics.Queries.GetPlatformMetrics;
using Application.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace WebApi.Controllers;

[ApiController]
[Route("api/admin/metrics")]
[Authorize("Admin")]
[EnableRateLimiting("partner-write")]
public sealed class AdminMetricsController(IMemoryCache cache) : ControllerBase
{
    private const string SummaryCacheKey = "admin-metrics-summary";
    private static readonly TimeSpan SummaryCacheDuration = TimeSpan.FromSeconds(30);

    // ADMIN_PROMPT.md §2.5: "тяжёлые сводки кешируй на короткое время" — this query fans out into
    // a dozen-plus aggregate queries (GetPlatformMetricsQueryHandler), so a short cache here means
    // the dashboard tab (which several Admin staff can have open at once) doesn't re-run all of them
    // on every render/poll; 30s keeps the numbers close to live without that cost.
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromServices] IQueryHandler<GetPlatformMetricsQuery, GetPlatformMetricsResult> handler,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(SummaryCacheKey, out GetPlatformMetricsResult? cached) && cached is not null)
            return Ok(cached);

        var result = await handler.Handle(new GetPlatformMetricsQuery(), cancellationToken);
        cache.Set(SummaryCacheKey, result, SummaryCacheDuration);
        return Ok(result);
    }

    [HttpGet("time-series")]
    public async Task<IActionResult> GetTimeSeries(
        DateOnly from,
        DateOnly to,
        [FromServices] IQueryHandler<GetMetricsTimeSeriesQuery, GetMetricsTimeSeriesResult> handler,
        [FromServices] IValidator<GetMetricsTimeSeriesQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetMetricsTimeSeriesQuery(from, to);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }
}
