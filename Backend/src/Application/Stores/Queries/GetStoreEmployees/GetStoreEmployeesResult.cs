using Domain.Stores;

namespace Application.Stores.Queries.GetStoreEmployees;

public sealed record StoreEmployeeDto(
    int StoreEmployeeId,
    string UserId,
    StoreEmployeeRole Role,
    DateTimeOffset AddedAt,
    decimal? MonthlySalaryAmount,
    string? MonthlySalaryCurrency,
    TimeOnly? ScheduleStart,
    TimeOnly? ScheduleEnd);

public enum GetStoreEmployeesOutcome
{
    Found,
    StoreNotFound,
    Forbidden
}

public sealed record GetStoreEmployeesResult(GetStoreEmployeesOutcome Outcome, IReadOnlyList<StoreEmployeeDto>? Employees);
