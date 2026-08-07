using Domain.Stores;

namespace Application.Stores.Queries.GetStoreEmployeesForAdmin;

public sealed record AdminStoreEmployeeDto(
    int StoreEmployeeId,
    string UserId,
    string? Email,
    StoreEmployeeRole Role,
    DateTimeOffset AddedAt,
    TimeOnly? ScheduleStart,
    TimeOnly? ScheduleEnd);

public enum GetStoreEmployeesForAdminOutcome
{
    Found,
    StoreNotFound
}

public sealed record GetStoreEmployeesForAdminResult(GetStoreEmployeesForAdminOutcome Outcome, IReadOnlyList<AdminStoreEmployeeDto>? Employees);
