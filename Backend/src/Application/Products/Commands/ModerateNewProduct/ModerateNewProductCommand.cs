namespace Application.Products.Commands.ModerateNewProduct;

/// <summary>RequireOwnSubmission=true restricts the caller to moderating only their own submission
/// (the StorePartner self-approve path) - Admin moderation passes false to act on anyone's.</summary>
public sealed record ModerateNewProductCommand(int ProductSubmissionId, bool Approve, string PerformedByUserId, string? Reason, bool RequireOwnSubmission);
