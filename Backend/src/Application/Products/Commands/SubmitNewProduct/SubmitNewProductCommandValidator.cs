using FluentValidation;

namespace Application.Products.Commands.SubmitNewProduct;

public sealed class SubmitNewProductCommandValidator : AbstractValidator<SubmitNewProductCommand>
{
    public SubmitNewProductCommandValidator()
    {
        RuleFor(x => x.Barcode)
            .NotEmpty()
            .Length(8, 13)
            .Matches("^[0-9]+$");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.BrandId).GreaterThan(0);
        RuleFor(x => x.CountryOfOrigin).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SubmittedByUserId).NotEmpty();
    }
}
