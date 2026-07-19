namespace Application.Products.Commands.RecordScan;

public enum RecordScanOutcome
{
    Recorded,
    ProductNotFound
}

public sealed record RecordScanResult(RecordScanOutcome Outcome, int? ScanId);
