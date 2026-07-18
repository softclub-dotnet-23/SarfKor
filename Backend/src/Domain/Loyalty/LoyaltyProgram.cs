using Domain.Common;

namespace Domain.Loyalty;

public class LoyaltyProgram : Entity
{
    public int StoreId { get; set; }
    public decimal PointsPerCurrencyUnit { get; set; }
    public decimal RedemptionRate { get; set; }
    public bool IsActive { get; set; }
}
