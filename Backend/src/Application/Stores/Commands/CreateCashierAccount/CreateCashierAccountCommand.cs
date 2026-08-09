namespace Application.Stores.Commands.CreateCashierAccount;

/// <summary>Owner-sets-up-a-cashier-on-the-spot, deliberately separate from
/// CreateStoreEmployeeInvitationCommand: a cashier hired in person doesn't need an email round-trip
/// to prove they own the address, and the owner handing them a working login right there is the
/// common real-world flow for a small shop. Unlike the invite mechanism, this DOES let the owner see
/// a password -- but only a freshly generated one for a brand-new account, never an existing user's.
/// ScheduleStart/ScheduleEnd is the "Смена" picklist on the frontend, resolved to a concrete time
/// range before this command is built.</summary>
public sealed record CreateCashierAccountCommand(
    int StoreId,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    TimeOnly? ScheduleStart,
    TimeOnly? ScheduleEnd,
    string PerformedByUserId,
    string? PerformedByIpAddress = null);
