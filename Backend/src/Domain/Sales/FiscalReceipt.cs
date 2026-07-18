using Domain.Common;

namespace Domain.Sales;

public class FiscalReceipt : Entity
{
    public int SaleTransactionId { get; set; }
    public required string FiscalNumber { get; set; }
    public string? QrCodeReference { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
}
