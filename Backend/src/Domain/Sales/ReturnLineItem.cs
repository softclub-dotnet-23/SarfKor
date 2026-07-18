using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Sales;

public class ReturnLineItem : Entity
{
    public int SaleReturnId { get; set; }
    public int SaleLineItemId { get; set; }
    public int Quantity { get; set; }
    public required Money RefundAmount { get; set; }
}
