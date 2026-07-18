using Domain.Common;

namespace Domain.Analytics;

public class Scan : Entity
{
    public int ProductId { get; set; }
    public string? UserId { get; set; }
    public int? StoreId { get; set; }
    public DateTimeOffset ScannedAt { get; set; }
}
