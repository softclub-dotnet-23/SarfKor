using Domain.Common;

namespace Domain.Stores;

public class StoreEmployee : Entity
{
    public int StoreId { get; set; }
    public required string UserId { get; set; }
    public StoreEmployeeRole Role { get; set; }
    public DateTimeOffset AddedAt { get; set; }
}
