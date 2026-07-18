using Domain.Common;

namespace Domain.Feedback;

public class ReviewReply : Entity
{
    public int ReviewId { get; set; }
    public required string StorePartnerUserId { get; set; }
    public required string Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
