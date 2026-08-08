using Application.Abstractions;
using Application.Common;
using Domain.Stores;

namespace Application.Stores.Queries.GetStoreEmployeeInvitationByToken;

public sealed class GetStoreEmployeeInvitationByTokenQueryHandler(
    IStoreEmployeeInvitationRepository invitationRepository,
    IStoreRepository storeRepository,
    IAuthService authService) : IQueryHandler<GetStoreEmployeeInvitationByTokenQuery, GetStoreEmployeeInvitationByTokenResult>
{
    private static readonly GetStoreEmployeeInvitationByTokenResult NotFoundResult =
        new(GetStoreEmployeeInvitationByTokenOutcome.NotFound, null, null, null, null, false);

    public async Task<GetStoreEmployeeInvitationByTokenResult> Handle(GetStoreEmployeeInvitationByTokenQuery query, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByTokenHashAsync(InviteToken.Hash(query.Token), cancellationToken);
        if (invitation is null)
            return NotFoundResult;

        if (invitation.Status == StoreEmployeeInvitationStatus.Accepted)
            return NotFoundResult with { Outcome = GetStoreEmployeeInvitationByTokenOutcome.Accepted };

        if (invitation.Status == StoreEmployeeInvitationStatus.Revoked)
            return NotFoundResult with { Outcome = GetStoreEmployeeInvitationByTokenOutcome.Revoked };

        // Two separate checks, not one: IsEffectivelyExpired only catches the window BEFORE
        // StoreEmployeeInvitationExpiryJob has run (Status still Pending, ExpiresAt already passed).
        // Once that background job flips Status to Expired, IsEffectivelyExpired's own
        // `Status == Pending` guard makes it return false again -- without this explicit branch the
        // invite fell through to "Valid" for any already-job-marked-expired token (confirmed live:
        // an invite past its ExpiresAt read back as Valid once the job had ticked past it).
        if (invitation.Status == StoreEmployeeInvitationStatus.Expired || invitation.IsEffectivelyExpired(DateTimeOffset.UtcNow))
            return NotFoundResult with { Outcome = GetStoreEmployeeInvitationByTokenOutcome.Expired };

        // Only a StorePartner invite has a store to resolve — a plain User/Admin invite (StoreId
        // null) skips this lookup entirely and reports StoreName null.
        string? storeName = null;
        if (invitation.StoreId is { } storeId)
        {
            var store = await storeRepository.GetByIdAsync(storeId, cancellationToken);
            if (store is null)
                return NotFoundResult;
            storeName = store.Name;
        }

        var existingUserId = await authService.FindUserIdByEmailAsync(invitation.Email, cancellationToken);

        return new GetStoreEmployeeInvitationByTokenResult(
            GetStoreEmployeeInvitationByTokenOutcome.Valid, invitation.InvitedRole, storeName, invitation.Email, invitation.Role,
            RequiresPassword: existingUserId is null);
    }
}
