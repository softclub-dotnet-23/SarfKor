using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Stores;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Stores.Commands.AdminCreateStorePartner;

public sealed class AdminCreateStorePartnerCommandHandler(
    IStoreOwnerInvitationRepository invitationRepository,
    IAuthService authService,
    IEmailSender emailSender,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<AdminCreateStorePartnerCommandHandler> logger) : ICommandHandler<AdminCreateStorePartnerCommand, AdminCreateStorePartnerResult>
{
    private static readonly TimeSpan InvitationLifespan = TimeSpan.FromMinutes(20);

    public async Task<AdminCreateStorePartnerResult> Handle(AdminCreateStorePartnerCommand command, CancellationToken cancellationToken)
    {
        // Unmasked, unlike ForgotPasswordCommandHandler's anti-enumeration silence — this endpoint
        // is Admin-only, so telling the Admin the real answer isn't an information-disclosure risk.
        if (await authService.FindUserIdByEmailAsync(command.Email, cancellationToken) is not null)
            return new AdminCreateStorePartnerResult(AdminCreateStorePartnerOutcome.EmailAlreadyRegistered, null);

        var code = OtpCode.Generate();
        var codeHash = OtpCode.Hash(command.Email, code);

        // Re-inviting the same email (e.g. the Admin mistyped the store name) reuses the pending
        // row and issues a fresh code, rather than piling up duplicate invitations — same "resend
        // is harmless" reasoning as AddStoreEmployeeCommandHandler.
        var invitation = await invitationRepository.GetPendingByEmailAsync(command.Email, cancellationToken);
        if (invitation is null)
        {
            invitation = new StoreOwnerInvitation
            {
                Email = command.Email,
                StoreName = command.StoreName,
                Address = command.Address,
                Location = new GeoLocation(command.Latitude, command.Longitude),
                CodeHash = codeHash,
                InvitedByUserId = command.AdminUserId,
                ExpiresAt = DateTimeOffset.UtcNow.Add(InvitationLifespan),
                CreatedAt = DateTimeOffset.UtcNow
            };
            invitationRepository.Add(invitation);
        }
        else
        {
            invitation.StoreName = command.StoreName;
            invitation.Address = command.Address;
            invitation.Location = new GeoLocation(command.Latitude, command.Longitude);
            invitation.CodeHash = codeHash;
            invitation.AttemptCount = 0;
            invitation.ExpiresAt = DateTimeOffset.UtcNow.Add(InvitationLifespan);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.AdminUserId,
            Action = "StoreOwnerInvitation.Created",
            EntityType = nameof(StoreOwnerInvitation),
            EntityId = invitation.Id,
            Details = command.Email,
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendStoreOwnerInvitationEmailAsync(command.Email, command.StoreName, code, cancellationToken);
        }
        catch (Exception ex)
        {
            // Swallowed on purpose, same reasoning as AddStoreEmployeeCommandHandler: a broken SMTP
            // setup must not turn "invite a store owner" into a 500 for the Admin.
            logger.LogError(ex, "Failed to send store owner invitation email");
        }

        return new AdminCreateStorePartnerResult(AdminCreateStorePartnerOutcome.Invited, invitation.Id);
    }
}
