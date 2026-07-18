using Domain.Feedback;

namespace Application.Abstractions;

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(int reportId, CancellationToken cancellationToken);
    void Add(Report report);
}
