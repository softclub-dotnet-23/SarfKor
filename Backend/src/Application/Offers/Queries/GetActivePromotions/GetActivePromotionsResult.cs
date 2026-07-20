using Domain.Offers;

namespace Application.Offers.Queries.GetActivePromotions;

public sealed record PromotionDto(
    int PromotionId,
    int? ProductId,
    int? CategoryId,
    PromotionDiscountType DiscountType,
    decimal DiscountValue,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);

public sealed record GetActivePromotionsResult(IReadOnlyList<PromotionDto> Promotions);
