using FluentValidation;

namespace Application.Identity.Commands.RecordUserConsent;

public sealed class RecordUserConsentCommandValidator : AbstractValidator<RecordUserConsentCommand>
{
    public RecordUserConsentCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
    }
}
