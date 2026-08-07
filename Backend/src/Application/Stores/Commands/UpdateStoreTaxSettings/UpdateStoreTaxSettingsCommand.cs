using Domain.Stores;

namespace Application.Stores.Commands.UpdateStoreTaxSettings;

public sealed record UpdateStoreTaxSettingsCommand(int StoreId, bool IsVatPayer, StoreTaxRegime TaxRegime, string PerformedByUserId, string? PerformedByIpAddress = null);
