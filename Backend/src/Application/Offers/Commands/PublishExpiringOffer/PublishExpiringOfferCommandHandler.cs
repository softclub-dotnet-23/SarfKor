using Application.Abstractions;
using Application.Common;
using Domain.Offers;
using Domain.ValueObjects;

namespace Application.Offers.Commands.PublishExpiringOffer;

public sealed class PublishExpiringOfferCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IProductRepository productRepository,
    IExpiringOfferRepository expiringOfferRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<PublishExpiringOfferCommand, PublishExpiringOfferResult>
{
    public async Task<PublishExpiringOfferResult> Handle(PublishExpiringOfferCommand command, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(command.StoreId, cancellationToken))
            return new PublishExpiringOfferResult(PublishExpiringOfferOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
            return new PublishExpiringOfferResult(PublishExpiringOfferOutcome.Forbidden, null);

        if (!await productRepository.ExistsAsync(command.ProductId, cancellationToken))
            return new PublishExpiringOfferResult(PublishExpiringOfferOutcome.ProductNotFound, null);

        var offer = new ExpiringOffer
        {
            StoreId = command.StoreId,
            ProductId = command.ProductId,
            OriginalPrice = new Money(command.OriginalPrice, command.Currency),
            DiscountedPrice = new Money(command.DiscountedPrice, command.Currency),
            ExpiresAt = command.ExpiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        expiringOfferRepository.Add(offer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PublishExpiringOfferResult(PublishExpiringOfferOutcome.Published, offer.Id);
    }
}
