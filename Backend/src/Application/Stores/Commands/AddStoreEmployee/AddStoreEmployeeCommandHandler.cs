using Application.Abstractions;
using Application.Common;
using Domain.Stores;
using Microsoft.Extensions.Logging;

namespace Application.Stores.Commands.AddStoreEmployee;

public sealed class AddStoreEmployeeCommandHandler(
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository,
    IStoreEmployeeInvitationRepository invitationRepository,
    IAuthService authService,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    ILogger<AddStoreEmployeeCommandHandler> logger) : ICommandHandler<AddStoreEmployeeCommand, AddStoreEmployeeResult>
{
    private const string StorePartnerRole = "StorePartner";
    private static readonly TimeSpan InvitationLifespan = TimeSpan.FromHours(24);

    public async Task<AddStoreEmployeeResult> Handle(AddStoreEmployeeCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new AddStoreEmployeeResult(AddStoreEmployeeOutcome.StoreNotFound, null);

        if (store.OwnerUserId != command.PerformedByUserId)
            return new AddStoreEmployeeResult(AddStoreEmployeeOutcome.Forbidden, null);

        // The owner only knows the cashier's email (there is no user directory to browse) — resolve
        // it to the real UserId here, since that's what StoreEmployee/JWT claims actually key on.
        var employeeUserId = await authService.FindUserIdByEmailAsync(command.EmployeeEmail, cancellationToken);
        if (employeeUserId is null)
            return await InviteAsync(store.Name, command, cancellationToken);

        var existing = await storeEmployeeRepository.GetByStoreIdAsync(command.StoreId, cancellationToken);
        if (existing.Any(e => e.UserId == employeeUserId))
            return new AddStoreEmployeeResult(AddStoreEmployeeOutcome.AlreadyEmployed, null);

        var employee = new StoreEmployee
        {
            StoreId = command.StoreId,
            UserId = employeeUserId,
            Role = command.Role,
            AddedAt = DateTimeOffset.UtcNow
        };

        storeEmployeeRepository.Add(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // [Authorize("StorePartner")] on every POS/inventory controller checks this coarse JWT role
        // claim before a request ever reaches a handler's fine-grained "owner or employee of *this*
        // store" check — without it, a newly-added cashier would be rejected at the attribute gate
        // and never even reach the use-case-level authorization this StoreEmployee row exists for.
        await authService.AssignRoleAsync(employeeUserId, StorePartnerRole, cancellationToken);

        return new AddStoreEmployeeResult(AddStoreEmployeeOutcome.Added, employee.Id);
    }

    // No account exists for this email yet — invite them instead of just failing. Re-using the same
    // pending invitation (rather than creating a duplicate row) makes re-clicking "Добавить" for the
    // same email a harmless resend.
    private async Task<AddStoreEmployeeResult> InviteAsync(string storeName, AddStoreEmployeeCommand command, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetPendingByStoreAndEmailAsync(command.StoreId, command.EmployeeEmail, cancellationToken);
        if (invitation is null)
        {
            invitation = new StoreEmployeeInvitation
            {
                StoreId = command.StoreId,
                Email = command.EmployeeEmail,
                Role = command.Role,
                Token = Guid.NewGuid().ToString("N"),
                InvitedByUserId = command.PerformedByUserId,
                ExpiresAt = DateTimeOffset.UtcNow.Add(InvitationLifespan),
                CreatedAt = DateTimeOffset.UtcNow
            };
            invitationRepository.Add(invitation);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        try
        {
            await emailSender.SendStoreEmployeeInviteEmailAsync(command.EmployeeEmail, storeName, invitation.Token, cancellationToken);
        }
        catch (Exception ex)
        {
            // Swallowed on purpose, same reasoning as ForgotPasswordCommandHandler: a broken SMTP
            // setup must not turn "invite a cashier" into a 500 for the store owner.
            logger.LogError(ex, "Failed to send store employee invite email");
        }

        return new AddStoreEmployeeResult(AddStoreEmployeeOutcome.Invited, null);
    }
}
