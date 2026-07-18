namespace Application.Products.Commands.ModerateNewProduct;

public enum ModerateNewProductOutcome
{
    Approved,
    Rejected,
    NotFound,
    AlreadyModerated
}

public sealed record ModerateNewProductResult(ModerateNewProductOutcome Outcome, int? ProductId);
