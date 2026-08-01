using FluentValidation;

namespace Application.Stores.Commands.AdminCreateStorePartner;

public sealed class AdminCreateStorePartnerCommandValidator : AbstractValidator<AdminCreateStorePartnerCommand>
{
    public AdminCreateStorePartnerCommandValidator()
    {
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.StoreName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}
