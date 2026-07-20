using Application.Common;
using Application.Customers.Commands.CreateCustomer;
using Application.Customers.Queries.GetCustomerByPhone;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize("StorePartner")]
public sealed class CustomersController : ControllerBase
{
    [HttpPost]
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
