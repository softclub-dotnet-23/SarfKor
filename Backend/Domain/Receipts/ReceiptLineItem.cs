using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Receipts;

public class ReceiptLineItem : Entity
{
    public int ReceiptId { get; set; }
    public int? ProductId { get; set; }
    public string? RecognizedName { get; set; }
    public int Quantity { get; set; }
    public required Money Price { get; set; }
}
