using Application.Abstractions;
using Application.Reputation;
using Application.Reputation.Commands.AdjustTrustScore;
using Domain.Auditing;
using Domain.Reputation;
using Moq;

namespace Application.Tests;

public class AdjustTrustScoreCommandHandlerTests
{
    private const string AdminUserId = "admin-1";
    private const string TargetUserId = "user-1";

    private readonly Mock<IContributorTrustScoreRepository> _trustScoreRepository = new();
    private readonly Mock<IContributorTrustScoreAdjustmentRepository> _adjustmentRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AdjustTrustScoreCommandHandler CreateHandler() =>
        new(_trustScoreRepository.Object, _adjustmentRepository.Object, _auditLogRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_NoExistingScore_CreatesOneStartingFromDefaultAndApplyingDelta()
    {
        _trustScoreRepository.Setup(r => r.GetByUserIdAsync(TargetUserId, It.IsAny<CancellationToken>())).ReturnsAsync((ContributorTrustScore?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new AdjustTrustScoreCommand(TargetUserId, 10, "manual bonus", AdminUserId), CancellationToken.None);

        Assert.Equal(TrustScoreFormula.DefaultScore + 10, result.NewScore);
        _trustScoreRepository.Verify(r => r.Add(It.Is<ContributorTrustScore>(s => s.UserId == TargetUserId)), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingScore_AppliesDeltaOnTopOfCurrentValue()
    {
        var score = new ContributorTrustScore { UserId = TargetUserId, Score = 40, UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1) };
        _trustScoreRepository.Setup(r => r.GetByUserIdAsync(TargetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(score);

        var handler = CreateHandler();
        var result = await handler.Handle(new AdjustTrustScoreCommand(TargetUserId, -8, "repeated bad submissions", AdminUserId), CancellationToken.None);

        Assert.Equal(32, result.NewScore);
        Assert.Equal(32, score.Score);
        _trustScoreRepository.Verify(r => r.Add(It.IsAny<ContributorTrustScore>()), Times.Never);
    }

    // A manual correction must never be silently overwritten by the automatic recalculation path —
    // recorded as IsManual=true so the two are distinguishable in history (ADMIN_PROMPT.md §2.4).
    [Fact]
    public async Task Handle_RecordsAdjustmentHistoryAsManualWithReasonAndAdmin()
    {
        _trustScoreRepository.Setup(r => r.GetByUserIdAsync(TargetUserId, It.IsAny<CancellationToken>())).ReturnsAsync((ContributorTrustScore?)null);

        var handler = CreateHandler();
        await handler.Handle(new AdjustTrustScoreCommand(TargetUserId, 5, "corrected after review", AdminUserId), CancellationToken.None);

        _adjustmentRepository.Verify(r => r.Add(It.Is<ContributorTrustScoreAdjustment>(
            a => a.UserId == TargetUserId && a.Delta == 5 && a.Reason == "corrected after review" && a.IsManual && a.PerformedByAdminUserId == AdminUserId)), Times.Once);
    }

    [Fact]
    public async Task Handle_RecordsAuditLogWithReasonAndIpAddress()
    {
        _trustScoreRepository.Setup(r => r.GetByUserIdAsync(TargetUserId, It.IsAny<CancellationToken>())).ReturnsAsync((ContributorTrustScore?)null);

        var handler = CreateHandler();
        await handler.Handle(new AdjustTrustScoreCommand(TargetUserId, 5, "corrected after review", AdminUserId, "198.51.100.4"), CancellationToken.None);

        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(
            a => a.Action == "ContributorTrustScore.Adjusted" && a.Reason == "corrected after review" && a.IpAddress == "198.51.100.4")), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
