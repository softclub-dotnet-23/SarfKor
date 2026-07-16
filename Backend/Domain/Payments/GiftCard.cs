using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Payments;

public class GiftCard : Entity
{
    public required string Code { get; set; }
    public required Money Balance { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
