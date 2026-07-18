namespace Application.Products.Commands.ModerateNewProduct;

public sealed record ModerateNewProductCommand(int ProductSubmissionId, bool Approve, string AdminUserId, string? Reason);
