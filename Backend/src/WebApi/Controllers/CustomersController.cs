using Application.Common;
using Application.Customers.Commands.CreateCustomer;
using Application.Customers.Queries.GetCustomerByPhone;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

// Customer is intentionally a platform-wide directory, not store-scoped — a walk-in with no prior
// relationship to this store still needs to be findable by phone (e.g. to attach store credit or
// loyalty for the first time), so results aren't filtered by store. Rate limiting is what actually
// closes the phone-enumeration gap here, not per-store filtering.
[ApiController]
[Route("api/customers")]
[Authorize("StorePartner")]
public sealed class CustomersController : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> Create(
        CreateCustomerRequest request,
        [FromServices] ICommandHandler<CreateCustomerCommand, CreateCustomerResult> handler,
        [FromServices] IValidator<CreateCustomerCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new CreateCustomerCommand(request.PhoneNumber, request.FullName);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(command, cancellationToken));
    }

    [HttpGet("by-phone/{phoneNumber}")]
    [EnableRateLimiting("contributions")]
    public async Task<IActionResult> GetByPhone(
        string phoneNumber,
        [FromServices] IQueryHandler<GetCustomerByPhoneQuery, GetCustomerByPhoneResult> handler,
        [FromServices] IValidator<GetCustomerByPhoneQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetCustomerByPhoneQuery(phoneNumber);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }
}
