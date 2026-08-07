using Application.Abstractions;
using Application.Common;

namespace Application.Stores.Queries.GetStoreEmployeesForAdmin;

// "Сотрудники" tab (ADMIN_PROMPT.md §3, store card) -- Admin-only, no ownership check (unlike
// GetStoreEmployeesQuery, which a StorePartner calls for their own store). Deliberately leaves
// MonthlySalary off the DTO -- support diagnostics has no business surfacing pay figures, same
// trust boundary spirit as GetStoreDiagnostics (§2.6).
public sealed class GetStoreEmployeesForAdminQueryHandler(
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository,
    IAuthService authService) : IQueryHandler<GetStoreEmployeesForAdminQuery, GetStoreEmployeesForAdminResult>
{
    public async Task<GetStoreEmployeesForAdminResult> Handle(GetStoreEmployeesForAdminQuery query, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(query.StoreId, cancellationToken))
            return new GetStoreEmployeesForAdminResult(GetStoreEmployeesForAdminOutcome.StoreNotFound, null);

        var employees = await storeEmployeeRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        var emailsByUserId = await authService.GetEmailsByUserIdsAsync(employees.Select(e => e.UserId).Distinct().ToList(), cancellationToken);

        var dtos = employees
            .Select(e => new AdminStoreEmployeeDto(
                e.Id, e.UserId, emailsByUserId.GetValueOrDefault(e.UserId), e.Role, e.AddedAt, e.ScheduleStart, e.ScheduleEnd))
            .ToList();

        return new GetStoreEmployeesForAdminResult(GetStoreEmployeesForAdminOutcome.Found, dtos);
    }
}
