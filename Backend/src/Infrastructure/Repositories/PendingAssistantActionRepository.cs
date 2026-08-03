using Application.Abstractions;
using Domain.Assistant;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PendingAssistantActionRepository(AppDbContext dbContext) : IPendingAssistantActionRepository
{
    public Task<PendingAssistantAction?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.PendingAssistantActions.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public void Add(PendingAssistantAction action) => dbContext.PendingAssistantActions.Add(action);
}
