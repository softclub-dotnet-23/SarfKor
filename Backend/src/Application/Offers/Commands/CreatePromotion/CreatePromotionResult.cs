namespace Application.Offers.Commands.CreatePromotion;

public enum CreatePromotionOutcome
{
    Created,
    StoreNotFound,
    Forbidden,
    ProductNotFound,
    CategoryNotFound
}

public sealed record CreatePromotionResult(CreatePromotionOutcome Outcome, int? PromotionId);
