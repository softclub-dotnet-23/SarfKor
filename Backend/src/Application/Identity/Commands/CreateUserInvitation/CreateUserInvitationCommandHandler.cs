using Application.Abstractions;
using Application.Common;
using Application.Stores;
using Domain.Auditing;
using Domain.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Identity.Commands.CreateUserInvitation;

public sealed class CreateUserInvitationCommandHandler(
    IStoreEmployeeInvitationRepository invitationRepository,
    IStoreRepository storeRepository,
    IAuthService authService,
    IUserProfileRepository userProfileRepository,
    IEmailSender emailSender,
    IAuditLogRepository auditLogRepository,
    IOptions<StoreEmployeeInvitationOptions> invitationOptions,
    IUnitOfWork unitOfWork,
    ILogger<CreateUserInvitationCommandHandler> logger)
    : ICommandHandler<CreateUserInvitationCommand, CreateUserInvitationResult>
{
    public async Task<CreateUserInvitationResult> Handle(CreateUserInvitationCommand command, CancellationToken cancellationToken)
    {
        // Use-case-level Admin check — the controller's [Authorize(Roles = "Admin")] is the first
        // gate, this is the second, independent one (task spec: "проверка и на эндпоинте, и на
        // уровне use-case").
        var performer = await authService.GetUserDetailAsync(command.PerformedByUserId, cancellationToken);
        if (performer is null || !performer.Roles.Contains("Admin"))
            return new CreateUserInvitationResult(CreateUserInvitationOutcome.Forbidden, null);

        // Validator already enforces StoreId iff InvitedRole == StorePartner — this only resolves
        // the store's name for the email/StoreName-in-response, no separate branch needed.
        string? storeName = null;
        if (command.StoreId is { } storeId)
        {
            var store = await storeRepository.GetByIdAsync(storeId, cancellationToken);
            if (store is null)
                return new CreateUserInvitationResult(CreateUserInvitationOutcome.StoreNotFound, null);
            storeName = store.Name;
        }

        var email = command.Email.Trim();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(invitationOptions.Value.ExpiryDays);
        var rawToken = InviteToken.Generate();

        // Never reveals whether an account already exists for this email at create time — every
        // branch below ends in "Sent" regardless (same email-enumeration principle as
        // CreateStoreEmployeeInvitationCommandHandler; the distinction only ever surfaces later,
        // to the invitee themselves, inside AcceptStoreEmployeeInvitationCommandHandler).
        var invitation = await invitationRepository.GetPendingByEmailAndRoleAsync(email, command.InvitedRole, command.StoreId, cancellationToken);
        if (invitation is null)
        {
            invitation = new StoreEmployeeInvitation
            {
                StoreId = command.StoreId,
                Email = email,
                // A StorePartner invite from /admin/users always makes the invitee the store's
                // Owner — inviting a Cashier is StaffPage's job (the store owner's own "Пригласить
                // кассира"), not this admin-wide screen's.
                Role = command.InvitedRole == "StorePartner" ? StoreEmployeeRole.Owner : null,
                InvitedRole = command.InvitedRole,
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
            // Re-inviting the same pending email+role — refresh, don't duplicate (also rotates the
            // token, so an old copy of this email lying around no longer works).
            invitation.TokenHash = InviteToken.Hash(rawToken);
            invitation.ExpiresAt = expiresAt;
            invitation.LastSentAt = now;
        }

        // Two SaveChanges, not one — the audit log's EntityId needs invitation.Id, which EF only
        // assigns once the first INSERT actually runs. Same sequencing as InviteAdminCommandHandler.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "UserInvitation.Created",
            EntityType = nameof(StoreEmployeeInvitation),
            EntityId = invitation.Id,
            Details = storeName is not null
                ? $"Invited {email} as {command.InvitedRole} (store: {storeName})"
                : $"Invited {email} as {command.InvitedRole}",
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = now
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var inviterProfile = await userProfileRepository.GetByUserIdAsync(command.PerformedByUserId, cancellationToken);
        var language = inviterProfile?.PreferredLanguage ?? "tg";

        try
        {
            await emailSender.SendInvitationEmailAsync(
                email, invitation.InvitedRole, storeName, invitation.Role, rawToken, invitationOptions.Value.ExpiryDays, language, cancellationToken);
        }
        catch (Exception ex)
        {
            // Same reasoning as every other invite/reset email in this codebase: a broken SMTP
            // setup must not turn "invite a user" into a 500 for the admin.
            logger.LogError(ex, "Failed to send user invite email");
        }

        return new CreateUserInvitationResult(CreateUserInvitationOutcome.Sent, invitation.Id, invitation.ExpiresAt);
    }
}
