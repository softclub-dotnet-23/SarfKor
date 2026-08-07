using Application.Abstractions;
using Application.Common;
using Domain.Stores;

namespace Application.Stores.Queries.GetStoreEmployeeInvitations;

public sealed class GetStoreEmployeeInvitationsQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStoreEmployeeInvitationRepository invitationRepository) : IQueryHandler<GetStoreEmployeeInvitationsQuery, GetStoreEmployeeInvitationsResult>
{
    public async Task<GetStoreEmployeeInvitationsResult> Handle(GetStoreEmployeeInvitationsQuery query, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
            return new GetStoreEmployeeInvitationsResult(GetStoreEmployeeInvitationsOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(query.StoreId, query.CallerUserId, cancellationToken))
            return new GetStoreEmployeeInvitationsResult(GetStoreEmployeeInvitationsOutcome.Forbidden, null);

        // Fetches every status and filters in-memory below, against the *effective* status rather
        // than the raw column — a Pending row past ExpiresAt must show as Expired here even before
        // the sweep background job gets to it, and filtering that in the DB query itself (on the
        // raw Status) would silently drop those rows from an Status=Expired request instead.
        var invitations = await invitationRepository.GetByStoreIdAsync(query.StoreId, null, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var dtos = invitations
            // A Pending row past ExpiresAt reads as "still pending" to the DB until the sweep job's
            // next run — show it as Expired to the owner immediately rather than waiting on that.
            .Select(i => new StoreEmployeeInvitationDto(
                i.Id,
                i.Email,
                i.Role,
                i.IsEffectivelyExpired(now) ? StoreEmployeeInvitationStatus.Expired : i.Status,
                i.ExpiresAt,
                i.CreatedAt,
                i.LastSentAt))
            .ToList();

        // Status filter is applied against the *effective* status, matching what the caller
        // actually asked to see (e.g. "Pending" shouldn't include ones that just ticked over to
        // effectively-expired).
        if (query.Status.HasValue)
            dtos = dtos.Where(d => d.Status == query.Status.Value).ToList();

        return new GetStoreEmployeeInvitationsResult(GetStoreEmployeeInvitationsOutcome.Found, dtos);
    }
}
