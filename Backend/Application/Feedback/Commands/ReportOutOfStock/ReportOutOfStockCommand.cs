namespace Application.Feedback.Commands.ReportOutOfStock;

public sealed record ReportOutOfStockCommand(string UserId, int ProductId, int? StoreId, string Description);
