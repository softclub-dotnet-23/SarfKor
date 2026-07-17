using FluentValidation;

namespace Application.Products.Queries.ScanBarcode;

public sealed class ScanBarcodeQueryValidator : AbstractValidator<ScanBarcodeQuery>
{
    public ScanBarcodeQueryValidator()
    {
        RuleFor(x => x.Barcode)
            .NotEmpty()
            .Length(8, 13)
            .Matches("^[0-9]+$");

        RuleFor(x => x.UserLatitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.UserLatitude.HasValue);

        RuleFor(x => x.UserLongitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.UserLongitude.HasValue);
    }
}
