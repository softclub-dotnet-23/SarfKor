using Domain.Common;

namespace Domain.Reputation;

public class ContributorTrustScore : Entity
{
    public required string UserId { get; set; }
    public double Score { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
