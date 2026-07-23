using Domain.Feedback;

namespace Application.Abstractions;

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(int reportId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Report>> GetPendingAsync(CancellationToken cancellationToken);
    void Add(Report report);
}
