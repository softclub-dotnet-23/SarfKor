using Domain.Security;

namespace Application.Abstractions;

public interface ISecurityEventRepository
{
    Task<IReadOnlyList<SecurityEvent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    void Add(SecurityEvent securityEvent);
}
