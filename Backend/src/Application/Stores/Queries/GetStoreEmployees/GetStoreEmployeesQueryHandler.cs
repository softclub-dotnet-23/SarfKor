using Application.Abstractions;
using Application.Common;

namespace Application.Stores.Queries.GetStoreEmployees;

public sealed class GetStoreEmployeesQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStoreEmployeeRepository storeEmployeeRepository,
    IAuthService authService) : IQueryHandler<GetStoreEmployeesQuery, GetStoreEmployeesResult>
{
    public async Task<GetStoreEmployeesResult> Handle(GetStoreEmployeesQuery query, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(query.StoreId, cancellationToken))
            return new GetStoreEmployeesResult(GetStoreEmployeesOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(query.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetStoreEmployeesResult(GetStoreEmployeesOutcome.Forbidden, null);

        var employees = await storeEmployeeRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        // Batched, not one email lookup per row — the list shows email as a column now (task spec:
        // no more raw user ids on screen), and this is exactly the N+1 GetEmailsByUserIdsAsync's own
        // doc comment already warns against.
        var emailsByUserId = await authService.GetEmailsByUserIdsAsync(employees.Select(e => e.UserId).ToList(), cancellationToken);
        var dtos = employees
            .Select(e => new StoreEmployeeDto(
                e.Id, e.UserId, e.Role, e.AddedAt,
                e.MonthlySalary?.Amount, e.MonthlySalary?.Currency,
                e.ScheduleStart, e.ScheduleEnd,
                e.FirstName, e.LastName,
                emailsByUserId.GetValueOrDefault(e.UserId),
                e.PhoneNumber, e.IsActive))
            .ToList();

        return new GetStoreEmployeesResult(GetStoreEmployeesOutcome.Found, dtos);
    }
}
