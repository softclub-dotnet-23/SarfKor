using FluentValidation;

namespace Application.Stores.Commands.UpdateStoreTaxSettings;

public sealed class UpdateStoreTaxSettingsCommandValidator : AbstractValidator<UpdateStoreTaxSettingsCommand>
{
    public UpdateStoreTaxSettingsCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.TaxRegime).IsInEnum();
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
