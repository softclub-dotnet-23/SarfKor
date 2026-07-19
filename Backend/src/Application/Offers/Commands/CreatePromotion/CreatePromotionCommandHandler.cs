using Application.Abstractions;
using Application.Common;
using Domain.Offers;

namespace Application.Offers.Commands.CreatePromotion;

public sealed class CreatePromotionCommandHandler(
    IStoreRepository storeRepository,
    IPromotionRepository promotionRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreatePromotionCommand, CreatePromotionResult>
{
    public async Task<CreatePromotionResult> Handle(CreatePromotionCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new CreatePromotionResult(CreatePromotionOutcome.StoreNotFound, null);

        if (store.OwnerUserId != command.PerformedByUserId)
            return new CreatePromotionResult(CreatePromotionOutcome.Forbidden, null);

        var promotion = new Promotion
        {
            StoreId = command.StoreId,
            ProductId = command.ProductId,
            CategoryId = command.CategoryId,
            DiscountType = command.DiscountType,
            DiscountValue = command.DiscountValue,
            StartsAt = command.StartsAt,
            EndsAt = command.EndsAt
        };

        promotionRepository.Add(promotion);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreatePromotionResult(CreatePromotionOutcome.Created, promotion.Id);
    }
}
