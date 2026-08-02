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

    /// <summary>The store that issued this card. Redemption is intentionally not restricted to
    /// this store — see GiftCardRedemption for the per-redemption store attribution.</summary>
    public int IssuingStoreId { get; set; }

    public string? IssuedByUserId { get; set; }
}
