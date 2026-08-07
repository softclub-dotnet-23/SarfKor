using Application.Abstractions;
using Application.Common;
using Domain.Stores;

namespace Application.Stores.Commands.RevokeStoreEmployeeInvitation;

public sealed class RevokeStoreEmployeeInvitationCommandHandler(
    IStoreEmployeeInvitationRepository invitationRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IUnitOfWork unitOfWork) : ICommandHandler<RevokeStoreEmployeeInvitationCommand, RevokeStoreEmployeeInvitationResult>
{
    public async Task<RevokeStoreEmployeeInvitationResult> Handle(RevokeStoreEmployeeInvitationCommand command, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByIdAsync(command.InvitationId, cancellationToken);
        if (invitation is null)
            return new RevokeStoreEmployeeInvitationResult(RevokeStoreEmployeeInvitationOutcome.NotFound);

        if (!await storeAccessAuthorizer.IsOwnerAsync(invitation.StoreId, command.PerformedByUserId, cancellationToken))
            return new RevokeStoreEmployeeInvitationResult(RevokeStoreEmployeeInvitationOutcome.Forbidden);

        if (invitation.Status != StoreEmployeeInvitationStatus.Pending)
            return new RevokeStoreEmployeeInvitationResult(RevokeStoreEmployeeInvitationOutcome.NotPending);

        invitation.Status = StoreEmployeeInvitationStatus.Revoked;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RevokeStoreEmployeeInvitationResult(RevokeStoreEmployeeInvitationOutcome.Revoked);
    }
}
