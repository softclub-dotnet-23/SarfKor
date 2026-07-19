using Domain.Offers;

namespace Application.Offers.Commands.CreatePromotion;

public sealed record CreatePromotionCommand(
    int StoreId,
    int? ProductId,
    int? CategoryId,
    PromotionDiscountType DiscountType,
    decimal DiscountValue,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string PerformedByUserId);
