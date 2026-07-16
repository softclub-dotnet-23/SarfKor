using Domain.Common;

namespace Domain.Engagement;

public class Favorite : Entity
{
    public required string UserId { get; set; }
    public FavoriteType Type { get; set; }
    public int EntityId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
