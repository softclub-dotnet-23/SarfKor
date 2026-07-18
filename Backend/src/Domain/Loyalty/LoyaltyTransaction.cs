using Domain.Common;

namespace Domain.Loyalty;

public class LoyaltyTransaction : Entity
{
    public int LoyaltyAccountId { get; set; }
    public int? SaleTransactionId { get; set; }
    public int PointsDelta { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
