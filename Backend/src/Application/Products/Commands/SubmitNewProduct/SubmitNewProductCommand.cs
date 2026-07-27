namespace Application.Products.Commands.SubmitNewProduct;

public sealed record SubmitNewProductCommand(
    string Barcode,
    string Name,
    int CategoryId,
    int BrandId,
    string CountryOfOrigin,
    string SubmittedByUserId);
