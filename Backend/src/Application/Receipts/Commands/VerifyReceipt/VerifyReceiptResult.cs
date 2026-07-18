namespace Application.Receipts.Commands.VerifyReceipt;

public enum VerifyReceiptOutcome
{
    Verified,
    Mismatched,
    NotFound,
    Forbidden,
    MissingStore,
    AlreadyProcessed
}

public sealed record ReceiptLineComparisonDto(int? ProductId, decimal ReceiptPrice, decimal? CurrentPrice, bool Matches);

public sealed record VerifyReceiptResult(VerifyReceiptOutcome Outcome, IReadOnlyList<ReceiptLineComparisonDto>? Lines);
