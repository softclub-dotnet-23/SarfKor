using Application.Abstractions;
using Application.Common;
using Domain.Stores;

namespace Application.Identity.Queries.GetUserInvitations;

/// <summary>Admin-only, platform-wide list backing /admin/users' merged users+pending-invitations
/// table — GetStoreEmployeeInvitationsQueryHandler's sibling for the un-scoped case (every
/// invitation, any store or none, instead of one store's).</summary>
public sealed class GetUserInvitationsQueryHandler(
    IStoreEmployeeInvitationRepository invitationRepository,
    IStoreRepository storeRepository,
    IAuthService authService) : IQueryHandler<GetUserInvitationsQuery, GetUserInvitationsResult>
{
    public async Task<GetUserInvitationsResult> Handle(GetUserInvitationsQuery query, CancellationToken cancellationToken)
    {
        var caller = await authService.GetUserDetailAsync(query.CallerUserId, cancellationToken);
        if (caller is null || !caller.Roles.Contains("Admin"))
            return new GetUserInvitationsResult(GetUserInvitationsOutcome.Forbidden, null);

        var invitations = await invitationRepository.GetAllAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var storeIds = invitations.Where(i => i.StoreId.HasValue).Select(i => i.StoreId!.Value).Distinct().ToList();
        var stores = storeIds.Count > 0
            ? (await storeRepository.GetByIdsAsync(storeIds, cancellationToken)).ToDictionary(s => s.Id, s => s.Name)
            : new Dictionary<int, string>();

        // A Pending row past ExpiresAt reads as "still pending" to the DB until the expiry-sweep
        // background job's next run — show it as Expired to the admin immediately, same reasoning
        // as GetStoreEmployeeInvitationsQueryHandler.
        var dtos = invitations
            .Select(i => new UserInvitationListItem(
                i.Id,
                i.Email,
                i.InvitedRole,
                i.StoreId,
                i.StoreId.HasValue && stores.TryGetValue(i.StoreId.Value, out var name) ? name : null,
                i.Role,
                i.IsEffectivelyExpired(now) ? StoreEmployeeInvitationStatus.Expired : i.Status,
                i.ExpiresAt,
                i.CreatedAt,
                i.LastSentAt))
            .ToList();

        return new GetUserInvitationsResult(GetUserInvitationsOutcome.Found, dtos);
    }
}
