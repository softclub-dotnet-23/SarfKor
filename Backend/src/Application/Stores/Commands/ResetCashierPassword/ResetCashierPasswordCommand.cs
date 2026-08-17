namespace Application.Stores.Commands.ResetCashierPassword;

/// <summary>"Сбросить пароль" on the employee card — generates a brand new temporary password for an
/// EXISTING employee's account and returns it once, same one-time-reveal contract as
/// CreateCashierAccountCommand. StoreEmployeeId, not UserId — the caller only ever has "this row in
/// this store's list", and resolving through it doubles as proving the target is actually this
/// store's employee before touching their account.</summary>
public sealed record ResetCashierPasswordCommand(int StoreEmployeeId, string PerformedByUserId, string? PerformedByIpAddress = null);
