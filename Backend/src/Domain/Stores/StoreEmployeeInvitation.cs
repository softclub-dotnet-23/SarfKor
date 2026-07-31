using Domain.Common;

namespace Domain.Stores;

public class StoreEmployeeInvitation : Entity
{
    public int StoreId { get; set; }
    public required string Email { get; set; }
    public StoreEmployeeRole Role { get; set; }
    public required string Token { get; set; }
    public required string InvitedByUserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
