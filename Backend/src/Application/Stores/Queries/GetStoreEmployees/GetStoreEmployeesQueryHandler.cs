using Application.Abstractions;
using Application.Common;

namespace Application.Stores.Queries.GetStoreEmployees;

public sealed class GetStoreEmployeesQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStoreEmployeeRepository storeEmployeeRepository) : IQueryHandler<GetStoreEmployeesQuery, GetStoreEmployeesResult>
{
    public async Task<GetStoreEmployeesResult> Handle(GetStoreEmployeesQuery query, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(query.StoreId, cancellationToken))
            return new GetStoreEmployeesResult(GetStoreEmployeesOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(query.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetStoreEmployeesResult(GetStoreEmployeesOutcome.Forbidden, null);

        var employees = await storeEmployeeRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        var dtos = employees.Select(e => new StoreEmployeeDto(e.Id, e.UserId, e.Role, e.AddedAt)).ToList();

        return new GetStoreEmployeesResult(GetStoreEmployeesOutcome.Found, dtos);
    }
}
