namespace Application.Products.Commands.RecordScan;

public enum RecordScanOutcome
{
    Recorded,
    ProductNotFound,
    StoreNotFound
}

public sealed record RecordScanResult(RecordScanOutcome Outcome, int? ScanId);
