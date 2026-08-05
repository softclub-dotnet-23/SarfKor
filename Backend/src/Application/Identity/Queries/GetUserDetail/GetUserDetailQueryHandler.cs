using Application.Abstractions;
using Application.Common;

namespace Application.Identity.Queries.GetUserDetail;

public sealed class GetUserDetailQueryHandler(
    IAuthService authService,
    IContributorTrustScoreRepository trustScoreRepository,
    IContributorTrustScoreAdjustmentRepository trustScoreAdjustmentRepository,
    IPriceEntryRepository priceEntryRepository,
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository) : IQueryHandler<GetUserDetailQuery, GetUserDetailResult>
{
    public async Task<GetUserDetailResult> Handle(GetUserDetailQuery query, CancellationToken cancellationToken)
    {
        var user = await authService.GetUserDetailAsync(query.UserId, cancellationToken);
        if (user is null)
            return new GetUserDetailResult(GetUserDetailOutcome.NotFound, query.UserId, null, null, false, null, null, [], null, 0, 0, 0, []);

        var trustScore = await trustScoreRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var (total, verified) = await priceEntryRepository.CountByUserIdAsync(query.UserId, cancellationToken);

        // "Жалобы на него" (ADMIN_PROMPT.md §2.3): Report has no direct "against this user" field —
        // the closest real signal is how many automatic trust-score penalties this user has actually
        // taken (see ReportOutOfStockCommandHandler.PenalizeAuthorAsync), so that's what's counted.
        var adjustments = await trustScoreAdjustmentRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var reportPenalties = adjustments.Count(a => !a.IsManual && a.Delta < 0);

        var ownedStores = await storeRepository.GetOwnedByUserIdAsync(query.UserId, cancellationToken);
        var employedAt = await storeEmployeeRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var employedStoreIds = employedAt.Select(e => e.StoreId).Except(ownedStores.Select(s => s.Id)).ToList();
        var employedStores = await storeRepository.GetByIdsAsync(employedStoreIds, cancellationToken);

        var attachments = ownedStores.Select(s => new UserStoreAttachmentDto(s.Id, s.Name, "Владелец"))
            .Concat(employedStores.Select(s => new UserStoreAttachmentDto(s.Id, s.Name, "Сотрудник")))
            .ToList();

        return new GetUserDetailResult(
            GetUserDetailOutcome.Found,
            user.UserId,
            user.Email,
            user.CreatedAt,
            user.IsBlocked,
            user.BlockedReason,
            user.BlockedAt,
            user.Roles,
            trustScore?.Score,
            total,
            verified,
            reportPenalties,
            attachments);
    }
}
