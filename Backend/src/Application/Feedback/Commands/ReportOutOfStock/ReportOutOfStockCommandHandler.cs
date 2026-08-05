using Application.Abstractions;
using Application.Common;
using Application.Reputation;
using Domain.Feedback;
using Domain.Reputation;

namespace Application.Feedback.Commands.ReportOutOfStock;

public sealed class ReportOutOfStockCommandHandler(
    IReportRepository reportRepository,
    IPriceEntryRepository priceEntryRepository,
    IContributorTrustScoreRepository trustScoreRepository,
    IContributorTrustScoreAdjustmentRepository trustScoreAdjustmentRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ReportOutOfStockCommand, ReportOutOfStockResult>
{
    public async Task<ReportOutOfStockResult> Handle(ReportOutOfStockCommand command, CancellationToken cancellationToken)
    {
        var report = new Report
        {
            UserId = command.UserId,
            ProductId = command.ProductId,
            StoreId = command.StoreId,
            Type = ReportType.OutOfStock,
            Description = command.Description,
            CreatedAt = DateTimeOffset.UtcNow
        };

        reportRepository.Add(report);

        // No manual admin review anymore (ADMIN_PROMPT.md §1) — the automatic replacement is a
        // small trust-score penalty against whoever most recently priced this product at this
        // store, the only identifiable "author" a Report can be about today. Store-level
        // accumulation (the "problem stores" dashboard signal) is a separate read against
        // IReportRepository.CountByStoreIdSinceAsync — see GetProblemStoresQuery — not duplicated
        // here.
        if (command.StoreId is int storeId)
        {
            var currentPriceEntry = await priceEntryRepository.GetLatestForStoreAsync(command.ProductId, storeId, cancellationToken);
            if (currentPriceEntry?.SubmittedByUserId is string authorUserId)
                await PenalizeAuthorAsync(authorUserId, report.Id, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReportOutOfStockResult(report.Id);
    }

    private async Task PenalizeAuthorAsync(string authorUserId, int reportId, CancellationToken cancellationToken)
    {
        var trustScore = await trustScoreRepository.GetByUserIdAsync(authorUserId, cancellationToken);
        if (trustScore is null)
        {
            trustScore = new ContributorTrustScore { UserId = authorUserId, Score = TrustScoreFormula.DefaultScore, UpdatedAt = DateTimeOffset.UtcNow };
            trustScoreRepository.Add(trustScore);
        }

        trustScore.Score += TrustScoreFormula.ReportAgainstAuthorDelta;
        trustScore.UpdatedAt = DateTimeOffset.UtcNow;

        trustScoreAdjustmentRepository.Add(new ContributorTrustScoreAdjustment
        {
            UserId = authorUserId,
            Delta = TrustScoreFormula.ReportAgainstAuthorDelta,
            Reason = $"Report #{reportId} filed against a price this user submitted",
            IsManual = false,
            OccurredAt = DateTimeOffset.UtcNow
        });
    }
}
