using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Stores;

public class Store : Entity
{
    public required string OwnerUserId { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public required GeoLocation Location { get; set; }
    public StoreStatus Status { get; set; }

    // Set once at creation, never touched again — the "подключений" half of the Admin dashboard's
    // connections/sales time series (GetMetricsTimeSeriesQuery) and the "дата подключения" store
    // list filter/sort.
    public DateTimeOffset ConnectedAt { get; set; }

    // Quick-glance context for the current Status without joining AuditLog — AuditLog remains the
    // full history/source of truth (who, when, before/after); these two just carry the latest.
    public string? StatusReason { get; set; }
    public DateTimeOffset? StatusChangedAt { get; set; }

    // General is the only regime IsVatPayer matters for (see StoreTaxRegime) — defaults preserve
    // pre-existing behavior (every store already had tax rates applied unconditionally).
    public bool IsVatPayer { get; set; } = true;
    public StoreTaxRegime TaxRegime { get; set; } = StoreTaxRegime.General;
}
