using Domain.Common;

namespace Domain.Feedback;

public class Review : Entity
{
    public required string UserId { get; set; }
    public int ProductId { get; set; }
    public int? StoreId { get; set; }
    public int Rating { get; set; }
    public required string Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
