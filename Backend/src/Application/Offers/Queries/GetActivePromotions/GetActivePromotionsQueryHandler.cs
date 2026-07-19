using Application.Abstractions;
using Application.Common;

namespace Application.Offers.Queries.GetActivePromotions;

public sealed class GetActivePromotionsQueryHandler(IPromotionRepository promotionRepository) : IQueryHandler<GetActivePromotionsQuery, GetActivePromotionsResult>
{
    public async Task<GetActivePromotionsResult> Handle(GetActivePromotionsQuery query, CancellationToken cancellationToken)
    {
        var promotions = await promotionRepository.GetActiveByStoreIdAsync(query.StoreId, DateTimeOffset.UtcNow, cancellationToken);
        var dtos = promotions
            .Select(p => new PromotionDto(p.Id, p.ProductId, p.CategoryId, p.DiscountType, p.DiscountValue, p.StartsAt, p.EndsAt))
            .ToList();

        return new GetActivePromotionsResult(dtos);
    }
}
