using FluentValidation;

namespace Application.Identity.Commands.UpdateUserAvatar;

public sealed class UpdateUserAvatarCommandValidator : AbstractValidator<UpdateUserAvatarCommand>
{
    public UpdateUserAvatarCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AvatarReference).NotEmpty();
    }
}
