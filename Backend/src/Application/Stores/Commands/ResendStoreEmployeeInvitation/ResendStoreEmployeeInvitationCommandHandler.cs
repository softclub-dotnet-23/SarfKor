using Application.Abstractions;
using Application.Common;
using Domain.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Stores.Commands.ResendStoreEmployeeInvitation;

public sealed class ResendStoreEmployeeInvitationCommandHandler(
    IStoreEmployeeInvitationRepository invitationRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStoreRepository storeRepository,
    IUserProfileRepository userProfileRepository,
    IEmailSender emailSender,
    IOptions<StoreEmployeeInvitationOptions> invitationOptions,
    IUnitOfWork unitOfWork,
    ILogger<ResendStoreEmployeeInvitationCommandHandler> logger)
    : ICommandHandler<ResendStoreEmployeeInvitationCommand, ResendStoreEmployeeInvitationResult>
{
    public async Task<ResendStoreEmployeeInvitationResult> Handle(ResendStoreEmployeeInvitationCommand command, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByIdAsync(command.InvitationId, cancellationToken);
        if (invitation is null)
            return new ResendStoreEmployeeInvitationResult(ResendStoreEmployeeInvitationOutcome.NotFound);

        if (!await storeAccessAuthorizer.IsOwnerAsync(invitation.StoreId, command.PerformedByUserId, cancellationToken))
            return new ResendStoreEmployeeInvitationResult(ResendStoreEmployeeInvitationOutcome.Forbidden);

        if (invitation.Status != StoreEmployeeInvitationStatus.Pending)
            return new ResendStoreEmployeeInvitationResult(ResendStoreEmployeeInvitationOutcome.NotPending);

        var store = await storeRepository.GetByIdAsync(invitation.StoreId, cancellationToken);
        if (store is null)
            return new ResendStoreEmployeeInvitationResult(ResendStoreEmployeeInvitationOutcome.NotFound);

        var now = DateTimeOffset.UtcNow;
        var rawToken = InviteToken.Generate();
        invitation.TokenHash = InviteToken.Hash(rawToken);
        invitation.ExpiresAt = now.AddDays(invitationOptions.Value.ExpiryDays);
        invitation.LastSentAt = now;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var inviterProfile = await userProfileRepository.GetByUserIdAsync(command.PerformedByUserId, cancellationToken);
        var language = inviterProfile?.PreferredLanguage ?? "tg";

        try
        {
            await emailSender.SendStoreEmployeeInviteEmailAsync(
                invitation.Email, store.Name, invitation.Role, rawToken, invitationOptions.Value.ExpiryDays, language, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resend store employee invite email");
        }

        return new ResendStoreEmployeeInvitationResult(ResendStoreEmployeeInvitationOutcome.Resent);
    }
}
