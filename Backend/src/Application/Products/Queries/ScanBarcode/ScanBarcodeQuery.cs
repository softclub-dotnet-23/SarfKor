namespace Application.Products.Queries.ScanBarcode;

// CallerUserId is null for anonymous consumers and always ignored by the Approved-store filter --
// it only ever widens results, letting a StorePartner/cashier see their own store's price even
// while that store is still Pending admin approval (Pending only hides a store from consumers).
public sealed record ScanBarcodeQuery(string Barcode, double? UserLatitude, double? UserLongitude, string? CallerUserId = null);
