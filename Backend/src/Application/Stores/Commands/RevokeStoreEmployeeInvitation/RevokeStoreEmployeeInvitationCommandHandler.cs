using Application.Abstractions;
using Application.Common;
using Domain.Stores;

namespace Application.Stores.Commands.RevokeStoreEmployeeInvitation;

public sealed class RevokeStoreEmployeeInvitationCommandHandler(
    IStoreEmployeeInvitationRepository invitationRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IAuthService authService,
    IUnitOfWork unitOfWork) : ICommandHandler<RevokeStoreEmployeeInvitationCommand, RevokeStoreEmployeeInvitationResult>
{
    public async Task<RevokeStoreEmployeeInvitationResult> Handle(RevokeStoreEmployeeInvitationCommand command, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByIdAsync(command.InvitationId, cancellationToken);
        if (invitation is null)
            return new RevokeStoreEmployeeInvitationResult(RevokeStoreEmployeeInvitationOutcome.NotFound);

        // Same authorization shape as Resend — see ResendStoreEmployeeInvitationCommandHandler.
        var authorized = invitation.StoreId is { } ownedStoreId && await storeAccessAuthorizer.IsOwnerAsync(ownedStoreId, command.PerformedByUserId, cancellationToken);
        if (!authorized)
        {
            var performer = await authService.GetUserDetailAsync(command.PerformedByUserId, cancellationToken);
            authorized = performer?.Roles.Contains("Admin") == true;
        }
        if (!authorized)
            return new RevokeStoreEmployeeInvitationResult(RevokeStoreEmployeeInvitationOutcome.Forbidden);

        if (invitation.Status != StoreEmployeeInvitationStatus.Pending)
            return new RevokeStoreEmployeeInvitationResult(RevokeStoreEmployeeInvitationOutcome.NotPending);

        invitation.Status = StoreEmployeeInvitationStatus.Revoked;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RevokeStoreEmployeeInvitationResult(RevokeStoreEmployeeInvitationOutcome.Revoked);
    }
}
