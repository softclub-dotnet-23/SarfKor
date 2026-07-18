using Domain.Common;

namespace Domain.Offers;

public class Promotion : Entity
{
    public int StoreId { get; set; }
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }
    public PromotionDiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
}
