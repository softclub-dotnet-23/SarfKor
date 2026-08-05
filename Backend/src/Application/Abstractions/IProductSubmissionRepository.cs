using Domain.Products;

namespace Application.Abstractions;

// Pure provenance log now (ADMIN_PROMPT.md §1) — every submission creates its Product in the same
// transaction, so there is nothing left to "get pending" or moderate.
public interface IProductSubmissionRepository
{
    void Add(ProductSubmission submission);
}
