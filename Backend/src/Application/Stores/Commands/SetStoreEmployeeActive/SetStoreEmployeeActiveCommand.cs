namespace Application.Stores.Commands.SetStoreEmployeeActive;

/// <summary>"Отключить" / reactivate on the employee card — pauses store access without deleting the
/// employment record or its sale/audit history. IsActive=false makes GetRoleAsync/IsEmployeeAsync/
/// GetMyStores stop counting this row (see StoreEmployeeRepository); the account itself is untouched
/// and can still log in and use any OTHER store/role it has.</summary>
public sealed record SetStoreEmployeeActiveCommand(int StoreEmployeeId, bool IsActive, string PerformedByUserId, string? PerformedByIpAddress = null);
