namespace Application.Catalog.Commands.CreateTaxRate;

public sealed record CreateTaxRateCommand(string Name, decimal Percentage, int? CategoryId);
