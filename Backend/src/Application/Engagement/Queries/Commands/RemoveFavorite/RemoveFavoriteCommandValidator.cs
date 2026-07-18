using FluentValidation;

namespace Application.Engagement.Commands.RemoveFavorite;

public sealed class RemoveFavoriteCommandValidator : AbstractValidator<RemoveFavoriteCommand>
{
    public RemoveFavoriteCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.EntityId).GreaterThan(0);
        RuleFor(x => x.Type).IsInEnum();
    }
}
