using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Offers;

public class ExpiringOffer : Entity
{
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public required Money OriginalPrice { get; set; }
    public required Money DiscountedPrice { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
