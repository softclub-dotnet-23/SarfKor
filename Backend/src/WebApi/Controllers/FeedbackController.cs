using System.Security.Claims;
using Application.Common;
using Application.Feedback.Commands.ReportOutOfStock;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class FeedbackController : ControllerBase
{
    [HttpPost("out-of-stock")]
    [EnableRateLimiting("contributions")]
    public async Task<IActionResult> ReportOutOfStock(
        ReportOutOfStockRequest request,
        [FromServices] ICommandHandler<ReportOutOfStockCommand, ReportOutOfStockResult> handler,
        [FromServices] IValidator<ReportOutOfStockCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ReportOutOfStockCommand(userId, request.ProductId, request.StoreId, request.Description);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return Ok(result);
    }
}
