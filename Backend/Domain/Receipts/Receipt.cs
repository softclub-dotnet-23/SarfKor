using Domain.Common;

namespace Domain.Receipts;

public class Receipt : Entity
{
    public required string UserId { get; set; }
    public int? StoreId { get; set; }
    public required string ImageReference { get; set; }
    public ReceiptVerificationStatus Status { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public List<ReceiptLineItem> Lines { get; set; } = [];
}
