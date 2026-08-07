using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Products;

// Provenance record only — "who introduced this product to the catalog and when." Every submission
// now creates its Product in the same transaction (ADMIN_PROMPT.md §1: no moderation queue, products
// publish immediately), so ProductId is always set going forward. Pre-existing rows get it
// backfilled by the AddSubscriptionsAndRemoveModeration migration's data pass.
public class ProductSubmission : Entity
{
    public required Barcode Barcode { get; set; }
    public required string Name { get; set; }
    public int CategoryId { get; set; }
    public int BrandId { get; set; }
    public required string CountryOfOrigin { get; set; }
    public required string SubmittedByUserId { get; set; }

    // Null only for pre-existing rows from the old moderation era whose submission was rejected
    // (see the AddSubscriptionsAndRemoveModeration migration's data pass) — never null for a
    // submission created after that migration, since every new one creates its Product in the same
    // transaction (SubmitNewProductCommandHandler).
    public int? ProductId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
