using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Products;

public class ProductSubmission : Entity
{
    public required Barcode Barcode { get; set; }
    public required string Name { get; set; }
    public int CategoryId { get; set; }
    public int BrandId { get; set; }
    public required string CountryOfOrigin { get; set; }
    public required string SubmittedByUserId { get; set; }
    public ProductSubmissionStatus Status { get; set; }
    public string? ModeratedByAdminUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModeratedAt { get; set; }
}
