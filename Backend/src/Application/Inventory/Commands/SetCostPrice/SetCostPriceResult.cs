namespace Application.Inventory.Commands.SetCostPrice;

public sealed record SetCostPriceResult(SetCostPriceOutcome Outcome, int? CostPriceId);
