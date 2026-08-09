using Domain.Stores;
using FluentValidation;

namespace Application.Stores.Commands.CreateStoreEmployeeInvitation;

public sealed class CreateStoreEmployeeInvitationCommandValidator : AbstractValidator<CreateStoreEmployeeInvitationCommand>
{
    public CreateStoreEmployeeInvitationCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        // Cashier is deliberately excluded: a cashier is now created directly with a generated
        // password (CreateCashierAccountCommand), never invited by email — a small shop's cashier
        // may have no email at all, which is exactly why that path exists. This mechanism stays for
        // co-owner invites only. Server-side, not just hidden on the frontend (task spec: "проверка
        // ... на уровне use-case, а не только по роли").
        RuleFor(x => x.Role).Equal(StoreEmployeeRole.Owner).WithMessage("Кассир создаётся напрямую, без приглашения по почте.");
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
