namespace Application.Stores.Commands.UpdateStoreTaxSettings;

public enum UpdateStoreTaxSettingsOutcome
{
    Updated,
    NotFound
}

public sealed record UpdateStoreTaxSettingsResult(UpdateStoreTaxSettingsOutcome Outcome);
