namespace Application.Payments.Commands.IssueStoreCredit;

public sealed record IssueStoreCreditCommand(int StoreId, int CustomerId, decimal Amount, string Currency, string PerformedByUserId);
