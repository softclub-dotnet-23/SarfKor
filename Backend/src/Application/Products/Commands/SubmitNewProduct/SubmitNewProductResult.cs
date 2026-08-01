namespace Application.Products.Commands.SubmitNewProduct;

public enum SubmitNewProductOutcome
{
    Submitted,
    Created,
    DuplicateBarcode,
    DuplicatePendingSubmission,
    CategoryNotFound,
    BrandNotFound
}

public sealed record SubmitNewProductResult(SubmitNewProductOutcome Outcome, int? ProductSubmissionId, int? ProductId = null);
