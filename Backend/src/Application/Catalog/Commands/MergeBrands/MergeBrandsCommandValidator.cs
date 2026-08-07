using FluentValidation;

namespace Application.Catalog.Commands.MergeBrands;

public sealed class MergeBrandsCommandValidator : AbstractValidator<MergeBrandsCommand>
{
    public MergeBrandsCommandValidator()
    {
        RuleFor(x => x.TargetBrandId).GreaterThan(0);
        RuleFor(x => x.SourceBrandIds).NotEmpty();
        RuleForEach(x => x.SourceBrandIds).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
