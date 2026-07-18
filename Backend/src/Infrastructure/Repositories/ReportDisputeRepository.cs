using Application.Abstractions;
using Domain.Feedback;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ReportDisputeRepository(AppDbContext dbContext) : IReportDisputeRepository
{
    public Task<ReportDispute?> GetByIdAsync(int reportDisputeId, CancellationToken cancellationToken) =>
        dbContext.ReportDisputes.FirstOrDefaultAsync(d => d.Id == reportDisputeId, cancellationToken);

    public async Task<IReadOnlyList<ReportDispute>> GetPendingAsync(CancellationToken cancellationToken) =>
        await dbContext.ReportDisputes.Where(d => d.Status == ReportDisputeStatus.Pending).ToListAsync(cancellationToken);

    public void Add(ReportDispute dispute) => dbContext.ReportDisputes.Add(dispute);
}
