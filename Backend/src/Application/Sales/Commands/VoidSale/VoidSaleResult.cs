namespace Application.Sales.Commands.VoidSale;

public sealed record VoidSaleResult(VoidSaleOutcome Outcome, DateTimeOffset? VoidedAt);
