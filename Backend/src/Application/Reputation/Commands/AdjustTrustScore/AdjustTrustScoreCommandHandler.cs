using Application.Abstractions;
using Application.Common;
using Application.Reputation;
using Domain.Auditing;
using Domain.Reputation;

namespace Application.Reputation.Commands.AdjustTrustScore;

public sealed class AdjustTrustScoreCommandHandler(
    IContributorTrustScoreRepository trustScoreRepository,
    IContributorTrustScoreAdjustmentRepository adjustmentRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<AdjustTrustScoreCommand, AdjustTrustScoreResult>
{
    public async Task<AdjustTrustScoreResult> Handle(AdjustTrustScoreCommand command, CancellationToken cancellationToken)
    {
        var trustScore = await trustScoreRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (trustScore is null)
        {
            trustScore = new ContributorTrustScore { UserId = command.UserId, Score = TrustScoreFormula.DefaultScore, UpdatedAt = DateTimeOffset.UtcNow };
            trustScoreRepository.Add(trustScore);
        }

        trustScore.Score += command.Delta;
        trustScore.UpdatedAt = DateTimeOffset.UtcNow;

        // IsManual=true — see ContributorTrustScoreAdjustment: this row is never touched by the
        // automatic recalculation path (ReportOutOfStockCommandHandler/SubmitPriceUpdateCommandHandler),
        // it only ever decays over time the same as everything else.
        adjustmentRepository.Add(new ContributorTrustScoreAdjustment
        {
            UserId = command.UserId,
            Delta = command.Delta,
            Reason = command.Reason,
            IsManual = true,
            PerformedByAdminUserId = command.PerformedByAdminUserId,
            OccurredAt = DateTimeOffset.UtcNow
        });

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByAdminUserId,
            Action = "ContributorTrustScore.Adjusted",
            EntityType = nameof(ContributorTrustScore),
            EntityId = trustScore.Id,
            Reason = command.Reason,
            Details = $"Delta {command.Delta:+0.##;-0.##} for user {command.UserId}",
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdjustTrustScoreResult(trustScore.Score);
    }
}
