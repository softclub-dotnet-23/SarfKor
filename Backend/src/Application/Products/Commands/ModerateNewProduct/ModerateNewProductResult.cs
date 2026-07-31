namespace Application.Products.Commands.ModerateNewProduct;

public enum ModerateNewProductOutcome
{
    Approved,
    Rejected,
    NotFound,
    AlreadyModerated,
    Forbidden,
    DuplicateBarcode
}

public sealed record ModerateNewProductResult(ModerateNewProductOutcome Outcome, int? ProductId);
