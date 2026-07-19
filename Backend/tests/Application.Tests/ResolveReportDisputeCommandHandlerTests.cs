using Application.Abstractions;
using Application.Feedback.Commands.ResolveReportDispute;
using Domain.Auditing;
using Domain.Feedback;
using Moq;

namespace Application.Tests;

public class ResolveReportDisputeCommandHandlerTests
{
    private const int DisputeId = 1;
    private const string AdminUserId = "admin-1";

    private readonly Mock<IReportDisputeRepository> _reportDisputeRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ResolveReportDisputeCommandHandler CreateHandler() => new(_reportDisputeRepository.Object, _auditLogRepository.Object, _unitOfWork.Object);

    private static ReportDispute CreateDispute(ReportDisputeStatus status) => new()
    {
        ReportId = 1,
        DisputedByUserId = "user-1",
        Reason = "Not accurate",
        Status = status,
        CreatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Handle_DisputeNotFound_ReturnsNotFound()
    {
        _reportDisputeRepository.Setup(r => r.GetByIdAsync(DisputeId, It.IsAny<CancellationToken>())).ReturnsAsync((ReportDispute?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResolveReportDisputeCommand(DisputeId, true, AdminUserId), CancellationToken.None);

        Assert.Equal(ResolveReportDisputeOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_AlreadyResolved_ReturnsAlreadyResolved()
    {
        _reportDisputeRepository
            .Setup(r => r.GetByIdAsync(DisputeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDispute(ReportDisputeStatus.Dismissed));

        var handler = CreateHandler();
        var result = await handler.Handle(new ResolveReportDisputeCommand(DisputeId, true, AdminUserId), CancellationToken.None);

        Assert.Equal(ResolveReportDisputeOutcome.AlreadyResolved, result.Outcome);
    }

    [Fact]
    public async Task Handle_Uphold_SetsUpheldAndWritesAuditLog()
    {
        var dispute = CreateDispute(ReportDisputeStatus.Pending);
        _reportDisputeRepository.Setup(r => r.GetByIdAsync(DisputeId, It.IsAny<CancellationToken>())).ReturnsAsync(dispute);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResolveReportDisputeCommand(DisputeId, true, AdminUserId), CancellationToken.None);

        Assert.Equal(ResolveReportDisputeOutcome.Upheld, result.Outcome);
        Assert.Equal(ReportDisputeStatus.Upheld, dispute.Status);
        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(a => a.Action == "ReportDispute.Upheld")), Times.Once);
    }
}
