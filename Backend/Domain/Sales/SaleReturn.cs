using Domain.Common;

namespace Domain.Sales;

public class SaleReturn : Entity
{
    public int SaleTransactionId { get; set; }
    public required string ProcessedByUserId { get; set; }
    public required string Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<ReturnLineItem> Lines { get; set; } = [];
}
