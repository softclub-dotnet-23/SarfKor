namespace Application.Products.Commands.SubmitNewProduct;

public enum SubmitNewProductOutcome
{
    Created,
    DuplicateBarcode,
    CategoryNotFound,
    BrandNotFound
}

public sealed record SubmitNewProductResult(SubmitNewProductOutcome Outcome, int? ProductSubmissionId, int? ProductId = null);
