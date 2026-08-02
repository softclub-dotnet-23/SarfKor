using FluentValidation;

namespace Application.Stores.Commands.ApproveStore;

public sealed class ApproveStoreCommandValidator : AbstractValidator<ApproveStoreCommand>
{
    public ApproveStoreCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
