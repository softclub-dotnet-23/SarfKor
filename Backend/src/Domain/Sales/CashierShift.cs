using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Sales;

public class CashierShift : Entity
{
    public int StoreId { get; set; }
    public required string CashierUserId { get; set; }
    public required Money OpeningCash { get; set; }
    public Money? ExpectedCash { get; set; }
    public Money? ClosingCash { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}
