using Domain.Common;

namespace Domain.Loyalty;

public class LoyaltyAccount : Entity
{
    public int CustomerId { get; set; }
    public int LoyaltyProgramId { get; set; }
    public int PointsBalance { get; set; }
}
