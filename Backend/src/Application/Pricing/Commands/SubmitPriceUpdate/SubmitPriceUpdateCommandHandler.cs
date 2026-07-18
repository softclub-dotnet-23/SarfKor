using Application.Abstractions;
using Application.Common;
using Domain.Pricing;
using Domain.Reputation;
using Domain.ValueObjects;

namespace Application.Pricing.Commands.SubmitPriceUpdate;

public sealed class SubmitPriceUpdateCommandHandler(
    IProductRepository productRepository,
    IStoreRepository storeRepository,
    IPriceEntryRepository priceEntryRepository,
    IContributorTrustScoreRepository trustScoreRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<SubmitPriceUpdateCommand, SubmitPriceUpdateResult?>
{
    private const double DefaultTrustScore = 50;

    public async Task<SubmitPriceUpdateResult?> Handle(SubmitPriceUpdateCommand command, CancellationToken cancellationToken)
    {
        if (!await productRepository.ExistsAsync(command.ProductId, cancellationToken))
            return null;

        if (!await storeRepository.ExistsAsync(command.StoreId, cancellationToken))
            return null;

        var priceEntry = new PriceEntry
        {
            ProductId = command.ProductId,
            StoreId = command.StoreId,
            Price = new Money(command.Price, command.Currency),
            SubmittedByUserId = command.UserId,
            RecordedAt = DateTimeOffset.UtcNow
        };
        priceEntryRepository.Add(priceEntry);

        var trustScore = await trustScoreRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (trustScore is null)
        {
            trustScore = new ContributorTrustScore
            {
                UserId = command.UserId,
                Score = DefaultTrustScore,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            trustScoreRepository.Add(trustScore);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SubmitPriceUpdateResult(priceEntry.Id, priceEntry.RecordedAt);
    }
}
