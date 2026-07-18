namespace Application.Pricing.Commands.SubmitPriceUpdate;

public sealed record SubmitPriceUpdateCommand(int ProductId, int StoreId, string UserId, decimal Price, string Currency);
