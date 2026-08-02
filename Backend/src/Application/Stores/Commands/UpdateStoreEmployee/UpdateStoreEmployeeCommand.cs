namespace Application.Stores.Commands.UpdateStoreEmployee;

// null on either pair clears it (no salary set / no schedule set). Role changes are deliberately
// out of scope here — no existing role-change command, and it's a separate decision.
public sealed record UpdateStoreEmployeeCommand(
    int StoreEmployeeId,
    decimal? MonthlySalaryAmount,
    string? MonthlySalaryCurrency,
    TimeOnly? ScheduleStart,
    TimeOnly? ScheduleEnd,
    string PerformedByUserId);
