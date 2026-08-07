namespace Application.Products.Queries.SearchProducts;

// Backs the frontend's shared entity picker (product search dropdown) — replaces manual numeric
// product-ID entry across the StorePartner/Cashier cabinets. StoreId is optional and, when given,
// annotates each result with that store's current price so the picker row can show it; the search
// itself is store-agnostic (products are a platform-wide catalog, not per-store).
public sealed record SearchProductsQuery(string? Search, int? CategoryId, int? StoreId, int Skip, int Take);
