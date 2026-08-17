namespace Application.Stores.Commands.UpdateStoreEmployee;

// null on either pair clears it (no salary set / no schedule set). Role changes are deliberately
// out of scope here — no existing role-change command, and it's a separate decision.
// FirstName/LastName/PhoneNumber null means "leave unchanged" (this command doubles as the
// "изменить" action on the redesigned cashier card, which only ever edits a subset of fields at once).
public sealed record UpdateStoreEmployeeCommand(
    int StoreEmployeeId,
    decimal? MonthlySalaryAmount,
    string? MonthlySalaryCurrency,
    TimeOnly? ScheduleStart,
    TimeOnly? ScheduleEnd,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string PerformedByUserId);
