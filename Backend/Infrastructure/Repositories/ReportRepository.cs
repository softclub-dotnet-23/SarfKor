using Application.Abstractions;
using Domain.Feedback;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ReportRepository(AppDbContext dbContext) : IReportRepository
{
    public Task<Report?> GetByIdAsync(int reportId, CancellationToken cancellationToken) =>
        dbContext.Reports.FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

    public void Add(Report report) => dbContext.Reports.Add(report);
}
