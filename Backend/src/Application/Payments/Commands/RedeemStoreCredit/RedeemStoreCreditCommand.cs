namespace Application.Payments.Commands.RedeemStoreCredit;

public sealed record RedeemStoreCreditCommand(int StoreId, int CustomerId, decimal Amount, string PerformedByUserId);
