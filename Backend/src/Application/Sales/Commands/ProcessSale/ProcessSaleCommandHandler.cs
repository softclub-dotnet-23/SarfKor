using Application.Abstractions;
using Application.Common;
using Domain.Inventory;
using Domain.Offers;
using Domain.Payments;
using Domain.Sales;
using Domain.ValueObjects;

namespace Application.Sales.Commands.ProcessSale;

public sealed class ProcessSaleCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IProductRepository productRepository,
    IPriceEntryRepository priceEntryRepository,
    IPromotionRepository promotionRepository,
    IProductBundleRepository productBundleRepository,
    IGiftCardRepository giftCardRepository,
    IGiftCardRedemptionRepository giftCardRedemptionRepository,
    ICustomerRepository customerRepository,
    IStoreCreditRepository storeCreditRepository,
    ISaleTransactionRepository saleTransactionRepository,
    IStockLevelRepository stockLevelRepository,
    IStockMovementRepository stockMovementRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ProcessSaleCommand, ProcessSaleResult>
{
    private sealed class InsufficientStockSignal(int productId) : Exception
    {
        public int ProductId { get; } = productId;
    }

    private sealed class InsufficientGiftCardBalanceSignal : Exception;

    private sealed class InsufficientStoreCreditBalanceSignal : Exception;

    public async Task<ProcessSaleResult> Handle(ProcessSaleCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new ProcessSaleResult(ProcessSaleOutcome.StoreNotFound, null, null, null, null);

        // The owner or any registered cashier of this store can ring up a sale — cost/profit
        // visibility is what's actually restricted (see SetCostPriceCommandHandler/GetProfitReportQueryHandler),
        // not the POS itself.
        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(command.StoreId, command.CashierUserId, cancellationToken))
            return new ProcessSaleResult(ProcessSaleOutcome.Forbidden, null, null, null, null);

        // ADMIN_PROMPT.md §2.1: a Suspended subscription (or an administratively Suspended/Blocked/
        // Archived store) closes the register — checked ahead of the idempotency lookup so a retry
        // of an already-blocked attempt is refused the same way a fresh one is.
        if (!await storeAccessAuthorizer.IsOperationalAsync(command.StoreId, cancellationToken))
            return new ProcessSaleResult(ProcessSaleOutcome.SubscriptionInactive, null, null, null, null);

        var existing = await saleTransactionRepository.GetByIdempotencyKeyAsync(command.StoreId, command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            // Retry of an already-processed sale — the original discount/gift-card decisions were
            // already applied and committed, so we don't know or re-derive them here; only the
            // idempotent total is guaranteed correct on replay.
            var existingTotal = existing.Lines.Sum(l => l.UnitPriceAtSale.Amount * l.Quantity);
            var existingLines = existing.Lines
                .Select(l => new ProcessSaleLineResultDto(l.Id, l.ProductId, l.Quantity, l.UnitPriceAtSale.Amount))
                .ToList();
            return new ProcessSaleResult(ProcessSaleOutcome.Completed, existing.Id, existingTotal, existing.Currency, null, Lines: existingLines);
        }

        var activePromotions = await promotionRepository.GetActiveByStoreIdAsync(command.StoreId, DateTimeOffset.UtcNow, cancellationToken);

        // Batched instead of one product + one price lookup per line — a sale with N lines
        // previously issued 2N+ queries.
        var requestedProductIds = command.Lines.Select(l => l.ProductId).Distinct().ToList();
        var productsById = (await productRepository.GetByIdsAsync(requestedProductIds, cancellationToken)).ToDictionary(p => p.Id);
        var latestPricesByProduct = (await priceEntryRepository.GetLatestForStoreForProductsAsync(command.StoreId, requestedProductIds, cancellationToken))
            .ToDictionary(p => p.ProductId);

        var resolvedLines = new List<(int ProductId, int Quantity, Money UnitPrice)>();

        foreach (var line in command.Lines)
        {
            if (!productsById.TryGetValue(line.ProductId, out var product))
                return new ProcessSaleResult(ProcessSaleOutcome.ProductNotFound, null, null, null, line.ProductId);

            if (!latestPricesByProduct.TryGetValue(line.ProductId, out var priceEntry))
                return new ProcessSaleResult(ProcessSaleOutcome.PriceNotFound, null, null, null, line.ProductId);

            var promotion = SelectPromotion(activePromotions, line.ProductId, product.CategoryId);
            var unitPrice = promotion is null ? priceEntry.Price : ApplyDiscount(priceEntry.Price, promotion, line.Quantity);

            resolvedLines.Add((line.ProductId, line.Quantity, unitPrice));
        }

        // A bundle purchase expands into one resolved line per component product, priced so the
        // components' total equals the bundle's own price (not the sum of their normal prices) —
        // after that, stock decrement and SaleLineItem creation below don't need to know bundles
        // exist at all.
        foreach (var bundleLine in command.BundleLines ?? [])
        {
            var bundle = await productBundleRepository.GetByIdAsync(bundleLine.ProductBundleId, cancellationToken);
            if (bundle is null || bundle.StoreId != command.StoreId)
                return new ProcessSaleResult(ProcessSaleOutcome.BundleNotFound, null, null, null, null);

            var componentWeights = new List<(int ProductId, int Quantity, decimal Weight)>();
            foreach (var item in bundle.Items)
            {
                var componentPrice = await priceEntryRepository.GetLatestForStoreAsync(item.ProductId, command.StoreId, cancellationToken);
                componentWeights.Add((item.ProductId, item.Quantity, componentPrice?.Price.Amount ?? 0));
            }

            var referenceTotal = componentWeights.Sum(c => c.Weight * c.Quantity);
            if (referenceTotal <= 0)
            {
                // No current prices to weight by — fall back to splitting evenly per unit instead
                // of failing the whole bundle purchase over missing PriceEntry rows.
                componentWeights = componentWeights.Select(c => (c.ProductId, c.Quantity, Weight: 1m)).ToList();
                referenceTotal = componentWeights.Sum(c => c.Weight * c.Quantity);
            }

            foreach (var component in componentWeights)
            {
                var allocatedUnitPrice = bundle.BundlePrice.Amount * component.Weight / referenceTotal;
                var totalQuantity = component.Quantity * bundleLine.Quantity;
                resolvedLines.Add((component.ProductId, totalQuantity, new Money(allocatedUnitPrice, bundle.BundlePrice.Currency)));
            }
        }

        var total = resolvedLines.Sum(l => l.UnitPrice.Amount * l.Quantity);

        // Gift card is validated up front, same as product/price — a bad code should fail before
        // any stock is touched, not halfway through the transaction.
        GiftCard? giftCard = null;
        decimal? giftCardAmountApplied = null;

        if (command.GiftCardCode is not null)
        {
            giftCard = await giftCardRepository.GetByCodeAsync(command.GiftCardCode, cancellationToken);
            if (giftCard is null)
                return new ProcessSaleResult(ProcessSaleOutcome.GiftCardNotFound, null, null, null, null);

            if (!giftCard.IsActive || (giftCard.ExpiresAt is not null && giftCard.ExpiresAt < DateTimeOffset.UtcNow))
                return new ProcessSaleResult(ProcessSaleOutcome.GiftCardNotUsable, null, null, null, null);

            giftCardAmountApplied = Math.Min(giftCard.Balance.Amount, total);
        }

        // Store credit belongs to a specific customer, unlike the anonymous, code-based gift card
        // — CustomerId must be resolvable and real before any money moves.
        StoreCredit? storeCredit = null;
        decimal? storeCreditAmountApplied = null;

        if (command.CustomerId is not null)
        {
            if (await customerRepository.GetByIdAsync(command.CustomerId.Value, cancellationToken) is null)
                return new ProcessSaleResult(ProcessSaleOutcome.CustomerNotFound, null, null, null, null);

            if (command.ApplyStoreCredit)
            {
                storeCredit = await storeCreditRepository.GetByStoreAndCustomerAsync(command.StoreId, command.CustomerId.Value, cancellationToken);
                if (storeCredit is not null)
                {
                    var remainingAfterGiftCard = total - (giftCardAmountApplied ?? 0m);
                    storeCreditAmountApplied = Math.Min(storeCredit.Balance.Amount, remainingAfterGiftCard);
                }
            }
        }

        SaleTransaction? saleTransaction = null;

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                foreach (var line in resolvedLines)
                {
                    var decremented = await stockLevelRepository.TryDecrementAsync(line.ProductId, command.StoreId, line.Quantity, ct);
                    if (!decremented)
                        throw new InsufficientStockSignal(line.ProductId);
                }

                saleTransaction = new SaleTransaction
                {
                    StoreId = command.StoreId,
                    CustomerId = command.CustomerId,
                    CashierUserId = command.CashierUserId,
                    IdempotencyKey = command.IdempotencyKey,
                    Currency = command.Currency,
                    Status = SaleStatus.Completed,
                    CreatedAt = DateTimeOffset.UtcNow,
                    GiftCardId = giftCard?.Id,
                    GiftCardAmountApplied = giftCardAmountApplied,
                    StoreCreditAmountApplied = storeCreditAmountApplied
                };

                foreach (var line in resolvedLines)
                {
                    saleTransaction.Lines.Add(new SaleLineItem
                    {
                        ProductId = line.ProductId,
                        Quantity = line.Quantity,
                        UnitPriceAtSale = line.UnitPrice
                    });
                }

                saleTransactionRepository.Add(saleTransaction);
                await unitOfWork.SaveChangesAsync(ct);

                foreach (var line in resolvedLines)
                {
                    stockMovementRepository.Add(new StockMovement
                    {
                        ProductId = line.ProductId,
                        StoreId = command.StoreId,
                        Type = StockMovementType.Sale,
                        QuantityDelta = -line.Quantity,
                        RelatedSaleTransactionId = saleTransaction.Id,
                        PerformedByUserId = command.CashierUserId,
                        OccurredAt = DateTimeOffset.UtcNow
                    });
                }

                // Atomic, race-safe debits — giftCardAmountApplied/storeCreditAmountApplied were
                // computed from a balance read before this transaction started, so a concurrent
                // redemption could have spent the card/credit in the meantime; the WHERE-guarded
                // update (not the earlier read) is what actually prevents over-spending.
                if (giftCard is not null && giftCardAmountApplied is > 0)
                {
                    var debited = await giftCardRepository.TryDebitAsync(giftCard.Id, giftCardAmountApplied.Value, ct);
                    if (!debited)
                        throw new InsufficientGiftCardBalanceSignal();

                    giftCardRedemptionRepository.Add(new GiftCardRedemption
                    {
                        GiftCardId = giftCard.Id,
                        StoreId = command.StoreId,
                        Amount = giftCardAmountApplied.Value,
                        SaleTransactionId = saleTransaction.Id,
                        RedeemedAt = DateTimeOffset.UtcNow
                    });
                }

                if (storeCredit is not null && storeCreditAmountApplied is > 0)
                {
                    var debited = await storeCreditRepository.TryDebitAsync(storeCredit.Id, storeCreditAmountApplied.Value, ct);
                    if (!debited)
                        throw new InsufficientStoreCreditBalanceSignal();
                }

                await unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);
        }
        catch (InsufficientStockSignal signal)
        {
            return new ProcessSaleResult(ProcessSaleOutcome.InsufficientStock, null, null, null, signal.ProductId);
        }
        catch (InsufficientGiftCardBalanceSignal)
        {
            return new ProcessSaleResult(ProcessSaleOutcome.GiftCardInsufficientBalance, null, null, null, null);
        }
        catch (InsufficientStoreCreditBalanceSignal)
        {
            return new ProcessSaleResult(ProcessSaleOutcome.StoreCreditInsufficientBalance, null, null, null, null);
        }

        var amountDue = total - (giftCardAmountApplied ?? 0m) - (storeCreditAmountApplied ?? 0m);
        var lines = saleTransaction!.Lines
            .Select(l => new ProcessSaleLineResultDto(l.Id, l.ProductId, l.Quantity, l.UnitPriceAtSale.Amount))
            .ToList();
        return new ProcessSaleResult(
            ProcessSaleOutcome.Completed, saleTransaction.Id, total, command.Currency, null,
            giftCardAmountApplied, amountDue, storeCreditAmountApplied, lines);
    }

    /// <summary>Product-specific promotion wins over category-wide, which wins over store-wide.</summary>
    private static Promotion? SelectPromotion(IReadOnlyList<Promotion> activePromotions, int productId, int categoryId) =>
        activePromotions.FirstOrDefault(p => p.ProductId == productId)
        ?? activePromotions.FirstOrDefault(p => p.ProductId is null && p.CategoryId == categoryId)
        ?? activePromotions.FirstOrDefault(p => p.ProductId is null && p.CategoryId is null);

    private static Money ApplyDiscount(Money original, Promotion promotion, int quantity) => promotion.DiscountType switch
    {
        PromotionDiscountType.PercentageOff => original with { Amount = original.Amount * (1 - promotion.DiscountValue / 100m) },
        PromotionDiscountType.FixedAmountOff => original with { Amount = Math.Max(0, original.Amount - promotion.DiscountValue) },
        // "Buy one get one": pay for half the units (rounded up), expressed as an effective
        // per-unit price so UnitPriceAtSale * Quantity — the invariant every report/refund relies
        // on — still gives the correct line total without special-casing BOGO everywhere else.
        PromotionDiscountType.BuyOneGetOne => original with { Amount = original.Amount * Math.Ceiling(quantity / 2m) / quantity },
        _ => original
    };
}
