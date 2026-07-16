using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Sales;

public class SaleLineItem : Entity
{
    public int SaleTransactionId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public required Money UnitPriceAtSale { get; set; }
}
