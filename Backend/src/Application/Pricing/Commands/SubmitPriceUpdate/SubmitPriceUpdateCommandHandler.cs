using Application.Abstractions;
using Application.Common;
using Application.Reputation;
using Domain.Pricing;
using Domain.Reputation;
using Domain.ValueObjects;

namespace Application.Pricing.Commands.SubmitPriceUpdate;

public sealed class SubmitPriceUpdateCommandHandler(
    IProductRepository productRepository,
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IPriceEntryRepository priceEntryRepository,
    IContributorTrustScoreRepository trustScoreRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<SubmitPriceUpdateCommand, SubmitPriceUpdateResult>
{
    public async Task<SubmitPriceUpdateResult> Handle(SubmitPriceUpdateCommand command, CancellationToken cancellationToken)
    {
        if (!await productRepository.ExistsAsync(command.ProductId, cancellationToken))
            return new SubmitPriceUpdateResult(SubmitPriceUpdateOutcome.ProductNotFound, null, null);

        if (!await storeRepository.ExistsAsync(command.StoreId, cancellationToken))
            return new SubmitPriceUpdateResult(SubmitPriceUpdateOutcome.StoreNotFound, null, null);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(command.StoreId, command.UserId, cancellationToken))
            return new SubmitPriceUpdateResult(SubmitPriceUpdateOutcome.Forbidden, null, null);

        if (!await storeAccessAuthorizer.IsOperationalAsync(command.StoreId, cancellationToken))
            return new SubmitPriceUpdateResult(SubmitPriceUpdateOutcome.SubscriptionInactive, null, null);

        var trustScore = await trustScoreRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (trustScore is null)
        {
            trustScore = new ContributorTrustScore
            {
                UserId = command.UserId,
                Score = TrustScoreFormula.DefaultScore,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            trustScoreRepository.Add(trustScore);
        }

        // Corroboration check (ADMIN_PROMPT.md §1/§2.4): does a prior submission for this exact
        // product/store, from a *different* user, agree closely enough with this one? If so both
        // sides of that agreement are trustworthy regardless of either author's raw score alone.
        var priorEntry = await priceEntryRepository.GetLatestForStoreAsync(command.ProductId, command.StoreId, cancellationToken);
        var corroborated = priorEntry is not null
            && priorEntry.SubmittedByUserId is not null
            && priorEntry.SubmittedByUserId != command.UserId
            && TrustScoreFormula.PricesCorroborate(priorEntry.Price.Amount, command.Price);

        var priceEntry = new PriceEntry
        {
            ProductId = command.ProductId,
            StoreId = command.StoreId,
            Price = new Money(command.Price, command.Currency),
            SubmittedByUserId = command.UserId,
            RecordedAt = DateTimeOffset.UtcNow,
            IsVerified = corroborated || trustScore.Score >= TrustScoreFormula.LowTrustThreshold
        };
        priceEntryRepository.Add(priceEntry);

        if (corroborated && priorEntry is not null && !priorEntry.IsVerified)
            priorEntry.IsVerified = true;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SubmitPriceUpdateResult(SubmitPriceUpdateOutcome.Submitted, priceEntry.Id, priceEntry.RecordedAt);
    }
}
