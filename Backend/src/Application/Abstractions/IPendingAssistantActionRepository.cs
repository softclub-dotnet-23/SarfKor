using Domain.Assistant;

namespace Application.Abstractions;

public interface IPendingAssistantActionRepository
{
    Task<PendingAssistantAction?> GetByIdAsync(int id, CancellationToken cancellationToken);
    void Add(PendingAssistantAction action);
}
