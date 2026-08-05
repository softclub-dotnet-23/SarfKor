namespace Application.Catalog.Commands.DeleteTaxRate;

public sealed record DeleteTaxRateCommand(int TaxRateId, string PerformedByUserId, string? PerformedByIpAddress = null);
