using Application.Abstractions;
using Application.Common;

namespace Application.Stores.Queries.GetStoreEmployees;

public sealed class GetStoreEmployeesQueryHandler(
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository) : IQueryHandler<GetStoreEmployeesQuery, GetStoreEmployeesResult>
{
    public async Task<GetStoreEmployeesResult> Handle(GetStoreEmployeesQuery query, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
            return new GetStoreEmployeesResult(GetStoreEmployeesOutcome.StoreNotFound, null);

        if (store.OwnerUserId != query.RequestedByUserId)
            return new GetStoreEmployeesResult(GetStoreEmployeesOutcome.Forbidden, null);

        var employees = await storeEmployeeRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        var dtos = employees.Select(e => new StoreEmployeeDto(e.Id, e.UserId, e.Role, e.AddedAt)).ToList();

        return new GetStoreEmployeesResult(GetStoreEmployeesOutcome.Found, dtos);
    }
}
