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
    IAuthService authService,
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

        // A store-scoped (StorePartner) invite can be resent by that store's own owner OR any
        // Admin (who may have created it from /admin/users in the first place); a platform-wide
        // (User/Admin) invite has no store owner to defer to, so only an Admin can touch it.
        var authorized = invitation.StoreId is { } ownedStoreId && await storeAccessAuthorizer.IsOwnerAsync(ownedStoreId, command.PerformedByUserId, cancellationToken);
        if (!authorized)
        {
            var performer = await authService.GetUserDetailAsync(command.PerformedByUserId, cancellationToken);
            authorized = performer?.Roles.Contains("Admin") == true;
        }
        if (!authorized)
            return new ResendStoreEmployeeInvitationResult(ResendStoreEmployeeInvitationOutcome.Forbidden);

        if (invitation.Status != StoreEmployeeInvitationStatus.Pending)
            return new ResendStoreEmployeeInvitationResult(ResendStoreEmployeeInvitationOutcome.NotPending);

        string? storeName = null;
        if (invitation.StoreId is { } storeId)
        {
            var store = await storeRepository.GetByIdAsync(storeId, cancellationToken);
            if (store is null)
                return new ResendStoreEmployeeInvitationResult(ResendStoreEmployeeInvitationOutcome.NotFound);
            storeName = store.Name;
        }

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
            await emailSender.SendInvitationEmailAsync(
                invitation.Email, invitation.InvitedRole, storeName, invitation.Role, rawToken, invitationOptions.Value.ExpiryDays, language, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resend invite email");
        }

        return new ResendStoreEmployeeInvitationResult(ResendStoreEmployeeInvitationOutcome.Resent);
    }
}
