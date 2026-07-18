using Application.Abstractions;
using Application.Feedback.Commands.ModerateReport;
using Domain.Auditing;
using Domain.Feedback;
using Moq;

namespace Application.Tests;

public class ModerateReportCommandHandlerTests
{
    private const int ReportId = 1;
    private const string AdminUserId = "admin-1";

    private readonly Mock<IReportRepository> _reportRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ModerateReportCommandHandler CreateHandler() => new(
        _reportRepository.Object,
        _auditLogRepository.Object,
        _unitOfWork.Object);

    private static Report CreateReport(ReportStatus status) => new()
    {
        UserId = "user-1",
        ProductId = 1,
        Type = ReportType.OutOfStock,
        Description = "test",
        Status = status,
        CreatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Handle_ReportNotFound_ReturnsNotFound()
    {
        _reportRepository.Setup(r => r.GetByIdAsync(ReportId, It.IsAny<CancellationToken>())).ReturnsAsync((Report?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new ModerateReportCommand(ReportId, true, AdminUserId, null), CancellationToken.None);

        Assert.Equal(ModerateReportOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_AlreadyModerated_ReturnsAlreadyModerated()
    {
        _reportRepository.Setup(r => r.GetByIdAsync(ReportId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateReport(ReportStatus.Resolved));

        var handler = CreateHandler();
        var result = await handler.Handle(new ModerateReportCommand(ReportId, true, AdminUserId, null), CancellationToken.None);

        Assert.Equal(ModerateReportOutcome.AlreadyModerated, result.Outcome);
        _auditLogRepository.Verify(r => r.Add(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Resolve_SetsResolvedStatusAndWritesAuditLog()
    {
        var report = CreateReport(ReportStatus.Pending);
        _reportRepository.Setup(r => r.GetByIdAsync(ReportId, It.IsAny<CancellationToken>())).ReturnsAsync(report);

        var handler = CreateHandler();
        var result = await handler.Handle(new ModerateReportCommand(ReportId, true, AdminUserId, "confirmed"), CancellationToken.None);

        Assert.Equal(ModerateReportOutcome.Resolved, result.Outcome);
        Assert.Equal(ReportStatus.Resolved, report.Status);
        Assert.Equal(AdminUserId, report.ResolvedByAdminUserId);
        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(a => a.Action == "Report.Resolved")), Times.Once);
    }

    [Fact]
    public async Task Handle_Reject_SetsRejectedStatusAndWritesAuditLog()
    {
        var report = CreateReport(ReportStatus.Pending);
        _reportRepository.Setup(r => r.GetByIdAsync(ReportId, It.IsAny<CancellationToken>())).ReturnsAsync(report);

        var handler = CreateHandler();
        var result = await handler.Handle(new ModerateReportCommand(ReportId, false, AdminUserId, "spam"), CancellationToken.None);

        Assert.Equal(ModerateReportOutcome.Rejected, result.Outcome);
        Assert.Equal(ReportStatus.Rejected, report.Status);
        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(a => a.Action == "Report.Rejected")), Times.Once);
    }
}
