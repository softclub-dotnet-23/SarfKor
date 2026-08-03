using FluentValidation;

namespace Application.Assistant.Commands.ConfirmAssistantAction;

public sealed class ConfirmAssistantActionCommandValidator : AbstractValidator<ConfirmAssistantActionCommand>
{
    public ConfirmAssistantActionCommandValidator()
    {
        RuleFor(x => x.PendingActionId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}
