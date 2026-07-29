using Application.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        // EnableRetryOnFailure requires user-initiated transactions to run inside the execution
        // strategy it configures — otherwise a retried transient failure mid-transaction throws
        // instead of retrying, per EF Core's own guard against that combination.
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(
            action,
            static async (ctx, act, ct) =>
            {
                await using var transaction = await ctx.Database.BeginTransactionAsync(ct);
                try
                {
                    await act(ct);
                    await transaction.CommitAsync(ct);
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
                return true;
            },
            verifySucceeded: null,
            cancellationToken);
    }
}
