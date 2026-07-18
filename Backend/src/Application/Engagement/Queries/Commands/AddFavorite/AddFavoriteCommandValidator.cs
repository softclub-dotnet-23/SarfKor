using FluentValidation;

namespace Application.Engagement.Commands.AddFavorite;

public sealed class AddFavoriteCommandValidator : AbstractValidator<AddFavoriteCommand>
{
    public AddFavoriteCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.EntityId).GreaterThan(0);
        RuleFor(x => x.Type).IsInEnum();
    }
}
