using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Stores;

public class StoreEmployee : Entity
{
    public int StoreId { get; set; }
    public required string UserId { get; set; }
    public StoreEmployeeRole Role { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    public Money? MonthlySalary { get; set; }

    // "Смена" on the create/edit-cashier form -- a picklist of shift presets on the frontend, sent
    // through as a concrete start/end pair; these two fields already existed for this before the
    // cashier-creation rework, just never populated by that path.
    public TimeOnly? ScheduleStart { get; set; }
    public TimeOnly? ScheduleEnd { get; set; }

    // Required for a Cashier created directly via CreateCashierAccountCommand (no email round-trip
    // to derive a display name from); left null for an Owner attached via the invite flow, which
    // still gets its display name from UserProfile.DisplayName the invitee typed in themselves.
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // Tajikistan mobile format, validated in CreateCashierAccountCommandValidator/
    // UpdateStoreEmployeeCommandValidator -- required for a directly-created Cashier, optional
    // (unset) for everyone else.
    public string? PhoneNumber { get; set; }

    // "Отключить" on the employee card -- a paused employment, not a deleted one: GetRoleAsync/
    // IsEmployeeAsync/GetMyStores stop counting this row for store access the moment it flips false,
    // but the record (and its sale/audit history) stays. Distinct from RemoveStoreEmployeeCommand's
    // hard delete, which still exists separately.
    public bool IsActive { get; set; } = true;
}
