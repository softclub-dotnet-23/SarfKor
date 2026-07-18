namespace Application.Products.Queries.ScanBarcode;

public sealed record ScanBarcodeQuery(string Barcode, double? UserLatitude, double? UserLongitude);
