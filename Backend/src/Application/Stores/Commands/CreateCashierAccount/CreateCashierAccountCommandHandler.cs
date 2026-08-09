using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Identity;
using Domain.Stores;
using Microsoft.Extensions.Logging;

namespace Application.Stores.Commands.CreateCashierAccount;

public sealed class CreateCashierAccountCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStoreEmployeeRepository storeEmployeeRepository,
    IUserProfileRepository userProfileRepository,
    IAuthService authService,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateCashierAccountCommandHandler> logger)
    : ICommandHandler<CreateCashierAccountCommand, CreateCashierAccountResult>
{
    public async Task<CreateCashierAccountResult> Handle(CreateCashierAccountCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new CreateCashierAccountResult(CreateCashierAccountOutcome.StoreNotFound);

        if (!await storeAccessAuthorizer.IsOwnerAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
            return new CreateCashierAccountResult(CreateCashierAccountOutcome.Forbidden);

        var email = command.Email.Trim();

        // Never resets an existing account's password -- if the email is already registered
        // (elsewhere on the platform, or already an employee here under a different flow), the
        // owner needs CreateStoreEmployeeInvitationCommand instead, which attaches without ever
        // touching that account's credentials.
        if (await authService.FindUserIdByEmailAsync(email, cancellationToken) is not null)
            return new CreateCashierAccountResult(CreateCashierAccountOutcome.EmailAlreadyRegistered);

        var password = GeneratedPassword.Generate();
        var registerResult = await authService.RegisterAsync(email, password, emailPreVerified: true, cancellationToken);
        if (registerResult.Auth is null)
        {
            logger.LogWarning("CreateCashierAccount: RegisterAsync failed for a pre-checked-available email");
            return new CreateCashierAccountResult(CreateCashierAccountOutcome.RegistrationFailed);
        }

        var userId = registerResult.Auth.UserId;
        var firstName = command.FirstName.Trim();
        var lastName = command.LastName.Trim();
        userProfileRepository.Add(new UserProfile { UserId = userId, DisplayName = $"{firstName} {lastName}" });
        var storeEmployee = new StoreEmployee
        {
            StoreId = command.StoreId,
            UserId = userId,
            Role = StoreEmployeeRole.Cashier,
            AddedAt = DateTimeOffset.UtcNow,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = command.PhoneNumber.Trim(),
            ScheduleStart = command.ScheduleStart,
            ScheduleEnd = command.ScheduleEnd,
        };
        storeEmployeeRepository.Add(storeEmployee);
        // Every store employee (Owner or Cashier sub-role) also gets the platform-wide StorePartner
        // Identity role -- same convention AcceptStoreEmployeeInvitationCommandHandler uses; the
        // Cashier/Owner distinction is enforced by StoreEmployee.Role, not by a separate Identity role.
        await authService.AssignRoleAsync(userId, "StorePartner", cancellationToken);
        // The owner set this password, not the cashier -- forced change on first login (task spec:
        // "Кассир обязан сменить пароль при первом входе").
        await authService.MarkMustChangePasswordAsync(userId, cancellationToken);

        // Two SaveChanges: the audit log's EntityId needs storeEmployee.Id, which EF only assigns
        // once this first INSERT actually runs. Reads it straight off the same tracked instance
        // afterward -- deliberately NOT re-querying via GetByStoreIdAsync/GetByUserIdAsync, which
        // materialize the full entity including the MonthlySalary complex property; a StoreEmployee
        // just created without a salary is exactly the shape that crashed those reads before
        // StoreEmployeeConfiguration.MonthlySalary got IsRequired(false) (see WORKLOG) -- no need to
        // find out here whether that fix also covers the write side.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "CashierAccount.Created",
            EntityType = nameof(StoreEmployee),
            EntityId = storeEmployee.Id,
            Details = $"Created cashier account {email} for store «{store.Name}» (id {command.StoreId})",
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow,
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateCashierAccountResult(CreateCashierAccountOutcome.Created, email, password);
    }
}
