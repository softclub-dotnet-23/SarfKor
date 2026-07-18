using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Notifications;

public class PriceAlert : Entity
{
    public required string UserId { get; set; }
    public int ProductId { get; set; }
    public required Money TargetPrice { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
