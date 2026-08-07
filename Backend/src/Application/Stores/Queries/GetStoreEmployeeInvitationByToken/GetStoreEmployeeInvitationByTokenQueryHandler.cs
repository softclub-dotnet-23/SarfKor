using Application.Abstractions;
using Application.Common;
using Domain.Stores;

namespace Application.Stores.Queries.GetStoreEmployeeInvitationByToken;

public sealed class GetStoreEmployeeInvitationByTokenQueryHandler(
    IStoreEmployeeInvitationRepository invitationRepository,
    IStoreRepository storeRepository,
    IAuthService authService) : IQueryHandler<GetStoreEmployeeInvitationByTokenQuery, GetStoreEmployeeInvitationByTokenResult>
{
    public async Task<GetStoreEmployeeInvitationByTokenResult> Handle(GetStoreEmployeeInvitationByTokenQuery query, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByTokenHashAsync(InviteToken.Hash(query.Token), cancellationToken);
        if (invitation is null)
            return new GetStoreEmployeeInvitationByTokenResult(GetStoreEmployeeInvitationByTokenOutcome.NotFound, null, null, null, false);

        if (invitation.Status == StoreEmployeeInvitationStatus.Accepted)
            return new GetStoreEmployeeInvitationByTokenResult(GetStoreEmployeeInvitationByTokenOutcome.Accepted, null, null, null, false);

        if (invitation.Status == StoreEmployeeInvitationStatus.Revoked)
            return new GetStoreEmployeeInvitationByTokenResult(GetStoreEmployeeInvitationByTokenOutcome.Revoked, null, null, null, false);

        if (invitation.IsEffectivelyExpired(DateTimeOffset.UtcNow))
            return new GetStoreEmployeeInvitationByTokenResult(GetStoreEmployeeInvitationByTokenOutcome.Expired, null, null, null, false);

        var store = await storeRepository.GetByIdAsync(invitation.StoreId, cancellationToken);
        if (store is null)
            return new GetStoreEmployeeInvitationByTokenResult(GetStoreEmployeeInvitationByTokenOutcome.NotFound, null, null, null, false);

        var existingUserId = await authService.FindUserIdByEmailAsync(invitation.Email, cancellationToken);

        return new GetStoreEmployeeInvitationByTokenResult(
            GetStoreEmployeeInvitationByTokenOutcome.Valid, store.Name, invitation.Email, invitation.Role, RequiresPassword: existingUserId is null);
    }
}
