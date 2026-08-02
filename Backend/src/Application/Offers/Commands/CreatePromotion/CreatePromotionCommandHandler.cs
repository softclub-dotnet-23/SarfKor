using Application.Abstractions;
using Application.Common;
using Domain.Offers;

namespace Application.Offers.Commands.CreatePromotion;

public sealed class CreatePromotionCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IPromotionRepository promotionRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreatePromotionCommand, CreatePromotionResult>
{
    public async Task<CreatePromotionResult> Handle(CreatePromotionCommand command, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(command.StoreId, cancellationToken))
            return new CreatePromotionResult(CreatePromotionOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
            return new CreatePromotionResult(CreatePromotionOutcome.Forbidden, null);

        if (command.ProductId.HasValue && !await productRepository.ExistsAsync(command.ProductId.Value, cancellationToken))
            return new CreatePromotionResult(CreatePromotionOutcome.ProductNotFound, null);

        if (command.CategoryId.HasValue && !await categoryRepository.ExistsAsync(command.CategoryId.Value, cancellationToken))
            return new CreatePromotionResult(CreatePromotionOutcome.CategoryNotFound, null);

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
