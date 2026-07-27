namespace Application.Catalog.Commands.UpdateTaxRate;

public sealed record UpdateTaxRateCommand(int TaxRateId, string Name, decimal Percentage, int? CategoryId);
