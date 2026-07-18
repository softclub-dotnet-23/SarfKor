using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Sales;

public class Commission : Entity
{
    public int SaleTransactionId { get; set; }
    public required string CashierUserId { get; set; }
    public required Money Amount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
