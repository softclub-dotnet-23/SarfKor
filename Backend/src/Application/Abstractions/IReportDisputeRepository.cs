using Domain.Feedback;

namespace Application.Abstractions;

public interface IReportDisputeRepository
{
    Task<ReportDispute?> GetByIdAsync(int reportDisputeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportDispute>> GetPendingAsync(CancellationToken cancellationToken);
    void Add(ReportDispute dispute);
}
