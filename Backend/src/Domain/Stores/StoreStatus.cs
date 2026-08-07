namespace Domain.Stores;

// Values 0/1 preserved exactly (PendingApproval == old Pending, Active == old Approved) so existing
// rows need no data migration for this column itself — every other value is additive.
public enum StoreStatus
{
    PendingApproval,
    Active,
    Suspended,
    Blocked,
    Archived,
    Rejected
}
