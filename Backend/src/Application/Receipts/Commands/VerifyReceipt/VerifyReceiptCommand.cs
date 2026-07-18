namespace Application.Receipts.Commands.VerifyReceipt;

public sealed record VerifyReceiptCommand(int ReceiptId, string RequestedByUserId);
