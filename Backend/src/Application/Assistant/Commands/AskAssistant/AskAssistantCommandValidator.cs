using FluentValidation;

namespace Application.Assistant.Commands.AskAssistant;

public sealed class AskAssistantCommandValidator : AbstractValidator<AskAssistantCommand>
{
    public AskAssistantCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.StoreId).GreaterThan(0).When(x => x.StoreId.HasValue);
        RuleFor(x => x.History).Must(h => h.Count <= 40).WithMessage("Слишком длинная история переписки.");
        RuleForEach(x => x.History).ChildRules(message =>
        {
            message.RuleFor(m => m.Role).Must(r => r is "user" or "assistant");
            message.RuleFor(m => m.Content).NotEmpty().MaximumLength(4000);
        });
    }
}
