using Application.Abstractions;
using Application.Common;
using Domain.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Stores.Commands.CreateStoreEmployeeInvitation;

public sealed class CreateStoreEmployeeInvitationCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStoreEmployeeRepository storeEmployeeRepository,
    IStoreEmployeeInvitationRepository invitationRepository,
    IUserProfileRepository userProfileRepository,
    IAuthService authService,
    IEmailSender emailSender,
    IOptions<StoreEmployeeInvitationOptions> invitationOptions,
    IUnitOfWork unitOfWork,
    ILogger<CreateStoreEmployeeInvitationCommandHandler> logger)
    : ICommandHandler<CreateStoreEmployeeInvitationCommand, CreateStoreEmployeeInvitationResult>
{
    public async Task<CreateStoreEmployeeInvitationResult> Handle(CreateStoreEmployeeInvitationCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new CreateStoreEmployeeInvitationResult(CreateStoreEmployeeInvitationOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
            return new CreateStoreEmployeeInvitationResult(CreateStoreEmployeeInvitationOutcome.Forbidden, null);

        if (!await storeAccessAuthorizer.IsOperationalAsync(command.StoreId, cancellationToken))
            return new CreateStoreEmployeeInvitationResult(CreateStoreEmployeeInvitationOutcome.SubscriptionInactive, null);

        var email = command.Email.Trim();

        // Already on this store's team — the owner already sees this in the employee list, so
        // surfacing it here isn't an email-enumeration leak (unlike "does this email have an
        // account anywhere on the platform", which this handler deliberately never checks/branches
        // on: every other case below sends an invitation and says Sent, whether or not an account
        // already exists for the address — see AcceptStoreEmployeeInvitationCommandHandler for
        // where that distinction actually gets resolved, after the invitee proves ownership).
        var existingUserId = await authService.FindUserIdByEmailAsync(email, cancellationToken);
        if (existingUserId is not null && await storeEmployeeRepository.IsEmployeeAsync(command.StoreId, existingUserId, cancellationToken))
            return new CreateStoreEmployeeInvitationResult(CreateStoreEmployeeInvitationOutcome.AlreadyEmployed, null);

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(invitationOptions.Value.ExpiryDays);
        var rawToken = InviteToken.Generate();

        var invitation = await invitationRepository.GetPendingByStoreAndEmailAsync(command.StoreId, email, cancellationToken);
        if (invitation is null)
        {
            invitation = new StoreEmployeeInvitation
            {
                StoreId = command.StoreId,
                Email = email,
                Role = command.Role,
                InvitedRole = "StorePartner",
                TokenHash = InviteToken.Hash(rawToken),
                InvitedByUserId = command.PerformedByUserId,
                ExpiresAt = expiresAt,
                CreatedAt = now,
                LastSentAt = now,
                Status = StoreEmployeeInvitationStatus.Pending
            };
            invitationRepository.Add(invitation);
        }
        else
        {
            // Re-inviting the same pending email — refresh, don't duplicate (also rotates the
            // token, so an old copy of this email lying around no longer works).
            invitation.Role = command.Role;
            invitation.TokenHash = InviteToken.Hash(rawToken);
            invitation.ExpiresAt = expiresAt;
            invitation.LastSentAt = now;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var inviterProfile = await userProfileRepository.GetByUserIdAsync(command.PerformedByUserId, cancellationToken);
        var language = inviterProfile?.PreferredLanguage ?? "tg";

        try
        {
            await emailSender.SendInvitationEmailAsync(
                email, invitation.InvitedRole, store.Name, invitation.Role, rawToken, invitationOptions.Value.ExpiryDays, language, cancellationToken);
        }
        catch (Exception ex)
        {
            // Same reasoning as every other invite/reset email in this codebase: a broken SMTP
            // setup must not turn "invite a cashier" into a 500 for the store owner.
            logger.LogError(ex, "Failed to send store employee invite email");
        }

        return new CreateStoreEmployeeInvitationResult(CreateStoreEmployeeInvitationOutcome.Sent, invitation.Id);
    }
}
