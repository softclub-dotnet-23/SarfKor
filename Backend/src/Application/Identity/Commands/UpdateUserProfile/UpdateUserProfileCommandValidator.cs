using FluentValidation;

namespace Application.Identity.Commands.UpdateUserProfile;

public sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        When(x => x.DisplayName is not null, () => RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100));
        When(x => x.PreferredLanguage is not null, () => RuleFor(x => x.PreferredLanguage).NotEmpty().Length(2, 5));
    }
}
