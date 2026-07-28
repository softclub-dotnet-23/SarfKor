namespace Application.Products.Queries.ScanBarcode;

public sealed record StorePriceDto(int StoreId, string StoreName, decimal Price, string Currency, double? DistanceKm, int PriceEntryId = 0);

public sealed record ScanBarcodeResult(int ProductId, string ProductName, IReadOnlyList<StorePriceDto> Stores);
