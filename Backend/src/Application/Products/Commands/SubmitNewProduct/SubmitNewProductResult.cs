namespace Application.Products.Commands.SubmitNewProduct;

public enum SubmitNewProductOutcome
{
    Submitted,
    DuplicateBarcode,
    DuplicatePendingSubmission,
    CategoryNotFound,
    BrandNotFound
}

public sealed record SubmitNewProductResult(SubmitNewProductOutcome Outcome, int? ProductSubmissionId);
