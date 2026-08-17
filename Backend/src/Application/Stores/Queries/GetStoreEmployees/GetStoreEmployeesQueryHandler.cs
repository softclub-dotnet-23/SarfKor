using Application.Abstractions;
using Application.Common;

namespace Application.Stores.Queries.GetStoreEmployees;

public sealed class GetStoreEmployeesQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStoreEmployeeRepository storeEmployeeRepository,
    IUserProfileRepository userProfileRepository,
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

        // Code review 2026-08-10 finding #5: FirstName/LastName are only ever populated for a
        // directly-created Cashier — a co-owner attached via the email-invite flow has always left
        // them null (their real name lives in UserProfile.DisplayName instead, from when they
        // accepted the invite). Without this fallback the redesigned list — whose whole point was
        // "never show raw ids" — still showed a truncated user-id fragment for every co-owner row.
        // Not worth a batched repository method: a store realistically has one or two owners, so
        // this is at most a couple of extra single-row lookups, not an N+1 over the employee list.
        var displayNamesByUserId = new Dictionary<string, string?>();
        foreach (var e in employees)
        {
            if (e.FirstName is not null || displayNamesByUserId.ContainsKey(e.UserId))
                continue;
            var profile = await userProfileRepository.GetByUserIdAsync(e.UserId, cancellationToken);
            displayNamesByUserId[e.UserId] = profile?.DisplayName;
        }

        var dtos = employees
            .Select(e => new StoreEmployeeDto(
                e.Id, e.UserId, e.Role, e.AddedAt,
                e.MonthlySalary?.Amount, e.MonthlySalary?.Currency,
                e.ScheduleStart, e.ScheduleEnd,
                e.FirstName ?? displayNamesByUserId.GetValueOrDefault(e.UserId),
                e.LastName,
                emailsByUserId.GetValueOrDefault(e.UserId),
                e.PhoneNumber, e.IsActive))
            .ToList();

        return new GetStoreEmployeesResult(GetStoreEmployeesOutcome.Found, dtos);
    }
}
