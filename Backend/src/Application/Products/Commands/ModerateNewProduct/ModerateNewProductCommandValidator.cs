using FluentValidation;

namespace Application.Products.Commands.ModerateNewProduct;

public sealed class ModerateNewProductCommandValidator : AbstractValidator<ModerateNewProductCommand>
{
    public ModerateNewProductCommandValidator()
    {
        RuleFor(x => x.ProductSubmissionId).GreaterThan(0);
        RuleFor(x => x.AdminUserId).NotEmpty();
    }
}
