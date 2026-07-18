namespace Application.Receipts.Commands.UploadReceipt;

public sealed record UploadReceiptLineInput(int? ProductId, string? RecognizedName, int Quantity, decimal Price, string Currency);

public sealed record UploadReceiptCommand(
    string UserId,
    int? StoreId,
    string ImageReference,
    IReadOnlyList<UploadReceiptLineInput> Lines);
