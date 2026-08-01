using Application.Abstractions;
using Application.Sales.Commands.ProcessSale;
using Domain.Catalog;
using Domain.Customers;
using Domain.Inventory;
using Domain.Offers;
using Domain.Payments;
using Domain.Pricing;
using Domain.Products;
using Domain.Sales;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;
using Xunit;

namespace Application.Tests;

public class ProcessSaleCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IPriceEntryRepository> _priceEntryRepository = new();
    private readonly Mock<IPromotionRepository> _promotionRepository = new();
    private readonly Mock<IProductBundleRepository> _productBundleRepository = new();
    private readonly Mock<IGiftCardRepository> _giftCardRepository = new();
    private readonly Mock<IGiftCardRedemptionRepository> _giftCardRedemptionRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IStoreCreditRepository> _storeCreditRepository = new();
    private readonly Mock<ISaleTransactionRepository> _saleTransactionRepository = new();
    private readonly Mock<IStockLevelRepository> _stockLevelRepository = new();
    private readonly Mock<IStockMovementRepository> _stockMovementRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public ProcessSaleCommandHandlerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        _storeAccessAuthorizer
            .Setup(a => a.IsOwnerOrEmployeeAsync(StoreId, OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // No active promotions by default — individual tests opt in where relevant.
        _promotionRepository
            .Setup(r => r.GetActiveByStoreIdAsync(StoreId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Batched product/price lookups default to empty — tests that need a line resolved opt in via SetupProductWithPrice.
        _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _priceEntryRepository
            .Setup(r => r.GetLatestForStoreForProductsAsync(StoreId, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private ProcessSaleCommandHandler CreateHandler() => new(
        _storeRepository.Object,
        _storeAccessAuthorizer.Object,
        _productRepository.Object,
        _priceEntryRepository.Object,
        _promotionRepository.Object,
        _productBundleRepository.Object,
        _giftCardRepository.Object,
        _giftCardRedemptionRepository.Object,
        _customerRepository.Object,
        _storeCreditRepository.Object,
        _saleTransactionRepository.Object,
        _stockLevelRepository.Object,
        _stockMovementRepository.Object,
        _unitOfWork.Object);

    private static Product CreateProduct(int id, int categoryId = 0) => new()
    {
        Id = id,
        Barcode = new Barcode("1234567890128"),
        Name = "Test product",
        CountryOfOrigin = "TJ",
        CategoryId = categoryId
    };

    /// <summary>Sets up the batched product+price lookup for a single product — the shape almost every
    /// non-bundle-line test needs (ProcessSaleCommandHandler resolves all command.Lines in one batch,
    /// not one query per line).</summary>
    private void SetupProductWithPrice(int productId, decimal price, string currency = "TJS", int categoryId = 0)
    {
        _productRepository
            .Setup(r => r.GetByIdsAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(productId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateProduct(productId, categoryId)]);
        _priceEntryRepository
            .Setup(r => r.GetLatestForStoreForProductsAsync(StoreId, It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(productId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PriceEntry { ProductId = productId, StoreId = StoreId, Price = new Money(price, currency) }]);
    }

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(999, OwnerId, "key-1", "TJS", [new ProcessSaleLine(1, 1)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, "someone-else", "key-1", "TJS", [new ProcessSaleLine(1, 1)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_RegisteredCashierNotOwner_IsAllowedToProcessSale()
    {
        const string cashierId = "cashier-1";
        _storeAccessAuthorizer
            .Setup(a => a.IsOwnerOrEmployeeAsync(StoreId, cashierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        SetupProductWithPrice(1, 10);
        _stockLevelRepository.Setup(r => r.TryDecrementAsync(1, StoreId, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, cashierId, "key-cashier", "TJS", [new ProcessSaleLine(1, 1)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.Completed, result.Outcome);
    }

    [Fact]
    public async Task Handle_DuplicateIdempotencyKey_ReturnsExistingSaleAndDoesNotTouchStock()
    {
        var existingSale = new SaleTransaction
        {
            StoreId = StoreId,
            CashierUserId = OwnerId,
            IdempotencyKey = "key-1",
            Currency = "TJS",
            Status = SaleStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            Lines = [new SaleLineItem { ProductId = 1, Quantity = 2, UnitPriceAtSale = new Money(10, "TJS") }]
        };

        _saleTransactionRepository
            .Setup(r => r.GetByIdempotencyKeyAsync(StoreId, "key-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSale);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-1", "TJS", [new ProcessSaleLine(1, 2)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.Completed, result.Outcome);
        Assert.Equal(existingSale.Id, result.SaleTransactionId);
        Assert.Equal(20m, result.TotalAmount);

        _stockLevelRepository.Verify(
            r => r.TryDecrementAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _saleTransactionRepository.Verify(r => r.Add(It.IsAny<SaleTransaction>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductDoesNotExist_ReturnsProductNotFound()
    {
        // Default constructor setup already returns an empty product list for any batch.
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-2", "TJS", [new ProcessSaleLine(1, 1)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.ProductNotFound, result.Outcome);
        Assert.Equal(1, result.FailedProductId);
    }

    [Fact]
    public async Task Handle_NoPriceEntry_ReturnsPriceNotFound()
    {
        _productRepository
            .Setup(r => r.GetByIdsAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(1)), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateProduct(1)]);
        // Price lookup stays empty (default constructor setup) — product exists, price doesn't.

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-3", "TJS", [new ProcessSaleLine(1, 1)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.PriceNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsInsufficientStockAndDoesNotCreateSale()
    {
        SetupProductWithPrice(1, 10);
        _stockLevelRepository
            .Setup(r => r.TryDecrementAsync(1, StoreId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-4", "TJS", [new ProcessSaleLine(1, 5)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.InsufficientStock, result.Outcome);
        Assert.Equal(1, result.FailedProductId);
        _saleTransactionRepository.Verify(r => r.Add(It.IsAny<SaleTransaction>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidSale_ReturnsCompletedWithCorrectTotal()
    {
        SetupProductWithPrice(1, 15);
        _stockLevelRepository
            .Setup(r => r.TryDecrementAsync(1, StoreId, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-5", "TJS", [new ProcessSaleLine(1, 3)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.Completed, result.Outcome);
        Assert.Equal(45m, result.TotalAmount);
        Assert.Null(result.GiftCardAmountApplied);
        Assert.Equal(45m, result.AmountDue);
        _saleTransactionRepository.Verify(r => r.Add(It.IsAny<SaleTransaction>()), Times.Once);
        _stockMovementRepository.Verify(r => r.Add(It.IsAny<StockMovement>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ActivePercentagePromotionOnProduct_DiscountsUnitPrice()
    {
        SetupProductWithPrice(1, 20);
        _promotionRepository
            .Setup(r => r.GetActiveByStoreIdAsync(StoreId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Promotion
            {
                StoreId = StoreId,
                ProductId = 1,
                DiscountType = PromotionDiscountType.PercentageOff,
                DiscountValue = 25,
                StartsAt = DateTimeOffset.UtcNow.AddDays(-1),
                EndsAt = DateTimeOffset.UtcNow.AddDays(1)
            }]);
        _stockLevelRepository
            .Setup(r => r.TryDecrementAsync(1, StoreId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-promo", "TJS", [new ProcessSaleLine(1, 2)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.Completed, result.Outcome);
        // 20 TJS unit price, 25% off => 15 per unit, x2 = 30, not 40.
        Assert.Equal(30m, result.TotalAmount);
    }

    [Fact]
    public async Task Handle_UnknownGiftCardCode_ReturnsGiftCardNotFoundAndDoesNotTouchStock()
    {
        SetupProductWithPrice(1, 10);
        _giftCardRepository.Setup(r => r.GetByCodeAsync("BADCODE", It.IsAny<CancellationToken>())).ReturnsAsync((GiftCard?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-gc1", "TJS", [new ProcessSaleLine(1, 1)], GiftCardCode: "BADCODE"),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.GiftCardNotFound, result.Outcome);
        _stockLevelRepository.Verify(r => r.TryDecrementAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExpiredGiftCard_ReturnsGiftCardNotUsable()
    {
        SetupProductWithPrice(1, 10);
        _giftCardRepository
            .Setup(r => r.GetByCodeAsync("EXPIRED", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GiftCard
            {
                Code = "EXPIRED",
                Balance = new Money(50, "TJS"),
                IsActive = true,
                IssuedAt = DateTimeOffset.UtcNow.AddDays(-100),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1)
            });

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-gc2", "TJS", [new ProcessSaleLine(1, 1)], GiftCardCode: "EXPIRED"),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.GiftCardNotUsable, result.Outcome);
    }

    [Fact]
    public async Task Handle_GiftCardBalanceCoversWholeSale_LeavesNothingDue()
    {
        SetupProductWithPrice(1, 10);
        var giftCard = new GiftCard { Id = 1, Code = "FULL50", Balance = new Money(50, "TJS"), IsActive = true, IssuedAt = DateTimeOffset.UtcNow };
        _giftCardRepository.Setup(r => r.GetByCodeAsync("FULL50", It.IsAny<CancellationToken>())).ReturnsAsync(giftCard);
        _giftCardRepository.Setup(r => r.TryDebitAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _stockLevelRepository.Setup(r => r.TryDecrementAsync(1, StoreId, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-gc3", "TJS", [new ProcessSaleLine(1, 2)], GiftCardCode: "FULL50"),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.Completed, result.Outcome);
        Assert.Equal(20m, result.TotalAmount);
        Assert.Equal(20m, result.GiftCardAmountApplied);
        Assert.Equal(0m, result.AmountDue);
        _giftCardRepository.Verify(r => r.TryDebitAsync(1, 20, It.IsAny<CancellationToken>()), Times.Once);
        _giftCardRedemptionRepository.Verify(r => r.Add(It.Is<GiftCardRedemption>(g => g.GiftCardId == 1 && g.Amount == 20)), Times.Once);
    }

    [Fact]
    public async Task Handle_GiftCardDebitLosesRace_ReturnsGiftCardInsufficientBalance()
    {
        SetupProductWithPrice(1, 10);
        var giftCard = new GiftCard { Id = 1, Code = "FULL50", Balance = new Money(50, "TJS"), IsActive = true, IssuedAt = DateTimeOffset.UtcNow };
        _giftCardRepository.Setup(r => r.GetByCodeAsync("FULL50", It.IsAny<CancellationToken>())).ReturnsAsync(giftCard);
        _giftCardRepository.Setup(r => r.TryDebitAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _stockLevelRepository.Setup(r => r.TryDecrementAsync(1, StoreId, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-gc3b", "TJS", [new ProcessSaleLine(1, 2)], GiftCardCode: "FULL50"),
            CancellationToken.None);

        // The real DB transaction rolls back the SaleTransaction insert along with the failed
        // gift-card debit; the mocked IUnitOfWork here just replays the callback inline with no
        // rollback semantics, so Add's call is still recorded — only the reported outcome matters.
        Assert.Equal(ProcessSaleOutcome.GiftCardInsufficientBalance, result.Outcome);
    }

    [Fact]
    public async Task Handle_GiftCardBalanceLowerThanTotal_AppliesOnlyWhatsAvailable()
    {
        SetupProductWithPrice(1, 10);
        var giftCard = new GiftCard { Id = 1, Code = "SMALL5", Balance = new Money(5, "TJS"), IsActive = true, IssuedAt = DateTimeOffset.UtcNow };
        _giftCardRepository.Setup(r => r.GetByCodeAsync("SMALL5", It.IsAny<CancellationToken>())).ReturnsAsync(giftCard);
        _giftCardRepository.Setup(r => r.TryDebitAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _stockLevelRepository.Setup(r => r.TryDecrementAsync(1, StoreId, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-gc4", "TJS", [new ProcessSaleLine(1, 2)], GiftCardCode: "SMALL5"),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.Completed, result.Outcome);
        Assert.Equal(20m, result.TotalAmount);
        Assert.Equal(5m, result.GiftCardAmountApplied);
        Assert.Equal(15m, result.AmountDue);
        _giftCardRepository.Verify(r => r.TryDebitAsync(1, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownCustomerId_ReturnsCustomerNotFound()
    {
        SetupProductWithPrice(1, 10);
        _customerRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-cust1", "TJS", [new ProcessSaleLine(1, 1)], CustomerId: 99),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.CustomerNotFound, result.Outcome);
        _stockLevelRepository.Verify(r => r.TryDecrementAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StoreCreditCoversPartOfSale_AppliesAvailableBalanceAndTagsCustomerOnSale()
    {
        SetupProductWithPrice(1, 10);
        _customerRepository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(new Customer { PhoneNumber = "+992900000000", CreatedAt = DateTimeOffset.UtcNow });
        var storeCredit = new StoreCredit { Id = 1, StoreId = StoreId, CustomerId = 7, Balance = new Money(8, "TJS"), UpdatedAt = DateTimeOffset.UtcNow };
        _storeCreditRepository.Setup(r => r.GetByStoreAndCustomerAsync(StoreId, 7, It.IsAny<CancellationToken>())).ReturnsAsync(storeCredit);
        _storeCreditRepository.Setup(r => r.TryDebitAsync(1, 8, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _stockLevelRepository.Setup(r => r.TryDecrementAsync(1, StoreId, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        SaleTransaction? added = null;
        _saleTransactionRepository.Setup(r => r.Add(It.IsAny<SaleTransaction>())).Callback<SaleTransaction>(s =>
        {
            s.Id = 1;
            added = s;
        });

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-cust2", "TJS", [new ProcessSaleLine(1, 2)], CustomerId: 7, ApplyStoreCredit: true),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.Completed, result.Outcome);
        Assert.Equal(20m, result.TotalAmount);
        Assert.Equal(8m, result.StoreCreditAmountApplied);
        Assert.Equal(12m, result.AmountDue);
        _storeCreditRepository.Verify(r => r.TryDebitAsync(1, 8, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(7, added!.CustomerId);
    }

    [Fact]
    public async Task Handle_StoreCreditDebitLosesRace_ReturnsStoreCreditInsufficientBalance()
    {
        SetupProductWithPrice(1, 10);
        _customerRepository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(new Customer { PhoneNumber = "+992900000000", CreatedAt = DateTimeOffset.UtcNow });
        var storeCredit = new StoreCredit { Id = 1, StoreId = StoreId, CustomerId = 7, Balance = new Money(8, "TJS"), UpdatedAt = DateTimeOffset.UtcNow };
        _storeCreditRepository.Setup(r => r.GetByStoreAndCustomerAsync(StoreId, 7, It.IsAny<CancellationToken>())).ReturnsAsync(storeCredit);
        _storeCreditRepository.Setup(r => r.TryDebitAsync(1, 8, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _stockLevelRepository.Setup(r => r.TryDecrementAsync(1, StoreId, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-cust2b", "TJS", [new ProcessSaleLine(1, 2)], CustomerId: 7, ApplyStoreCredit: true),
            CancellationToken.None);

        // See the comment on Handle_GiftCardDebitLosesRace_ReturnsGiftCardInsufficientBalance —
        // the mocked transaction doesn't roll back, so only the outcome is meaningful here.
        Assert.Equal(ProcessSaleOutcome.StoreCreditInsufficientBalance, result.Outcome);
    }

    [Fact]
    public async Task Handle_GiftCardAndStoreCreditBothApplied_StackTowardsTheSameTotal()
    {
        SetupProductWithPrice(1, 10);
        _customerRepository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(new Customer { PhoneNumber = "+992900000000", CreatedAt = DateTimeOffset.UtcNow });
        var giftCard = new GiftCard { Id = 1, Code = "STACK10", Balance = new Money(10, "TJS"), IsActive = true, IssuedAt = DateTimeOffset.UtcNow };
        _giftCardRepository.Setup(r => r.GetByCodeAsync("STACK10", It.IsAny<CancellationToken>())).ReturnsAsync(giftCard);
        _giftCardRepository.Setup(r => r.TryDebitAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var storeCredit = new StoreCredit { Id = 2, StoreId = StoreId, CustomerId = 7, Balance = new Money(100, "TJS"), UpdatedAt = DateTimeOffset.UtcNow };
        _storeCreditRepository.Setup(r => r.GetByStoreAndCustomerAsync(StoreId, 7, It.IsAny<CancellationToken>())).ReturnsAsync(storeCredit);
        _storeCreditRepository.Setup(r => r.TryDebitAsync(2, 20, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _stockLevelRepository.Setup(r => r.TryDecrementAsync(1, StoreId, 3, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();

        // Total = 30. Gift card covers 10, store credit should only take the remaining 20, not its full 100 balance.
        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-cust3", "TJS", [new ProcessSaleLine(1, 3)], GiftCardCode: "STACK10", CustomerId: 7, ApplyStoreCredit: true),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.Completed, result.Outcome);
        Assert.Equal(30m, result.TotalAmount);
        Assert.Equal(10m, result.GiftCardAmountApplied);
        Assert.Equal(20m, result.StoreCreditAmountApplied);
        Assert.Equal(0m, result.AmountDue);
        _storeCreditRepository.Verify(r => r.TryDebitAsync(2, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownBundleId_ReturnsBundleNotFound()
    {
        _productBundleRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((ProductBundle?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-bundle1", "TJS", [], BundleLines: [new ProcessSaleBundleLine(1, 1)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.BundleNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_BundleFromDifferentStore_ReturnsBundleNotFound()
    {
        _productBundleRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductBundle { StoreId = 999, Name = "Combo", BundlePrice = new Money(10, "TJS") });

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-bundle2", "TJS", [], BundleLines: [new ProcessSaleBundleLine(1, 1)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.BundleNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_BundlePurchase_AllocatesBundlePriceAcrossComponentsByCurrentPriceAndDecrementsEachComponentStock()
    {
        // Bundle "Combo" = 2x product 1 (normal 10/ea) + 1x product 2 (normal 20/ea), bundle price 30 (a discount off the normal 40).
        var bundle = new ProductBundle
        {
            StoreId = StoreId,
            Name = "Combo",
            BundlePrice = new Money(30, "TJS"),
            Items =
            [
                new ProductBundleItem { ProductId = 1, Quantity = 2 },
                new ProductBundleItem { ProductId = 2, Quantity = 1 }
            ]
        };
        _productBundleRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(bundle);
        _priceEntryRepository
            .Setup(r => r.GetLatestForStoreAsync(1, StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceEntry { ProductId = 1, StoreId = StoreId, Price = new Money(10, "TJS") });
        _priceEntryRepository
            .Setup(r => r.GetLatestForStoreAsync(2, StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceEntry { ProductId = 2, StoreId = StoreId, Price = new Money(20, "TJS") });
        _stockLevelRepository.Setup(r => r.TryDecrementAsync(1, StoreId, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _stockLevelRepository.Setup(r => r.TryDecrementAsync(2, StoreId, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-bundle3", "TJS", [], BundleLines: [new ProcessSaleBundleLine(1, 1)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.Completed, result.Outcome);
        // Reference weights: product1 = 10*2=20, product2 = 20*1=20, total=40. Bundle price 30 split 20:20 => 15:15.
        // Product1 unit price = 15/20*10=7.5 (x2 units=15); product2 unit price = 15/20*20=15 (x1 unit=15). Total=30.
        Assert.Equal(30m, result.TotalAmount);
        _stockLevelRepository.Verify(r => r.TryDecrementAsync(1, StoreId, 2, It.IsAny<CancellationToken>()), Times.Once);
        _stockLevelRepository.Verify(r => r.TryDecrementAsync(2, StoreId, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BundlePurchase_MultipleQuantities_ScalesComponentDecrementsLinearly()
    {
        var bundle = new ProductBundle
        {
            StoreId = StoreId,
            Name = "Combo",
            BundlePrice = new Money(10, "TJS"),
            Items = [new ProductBundleItem { ProductId = 1, Quantity = 2 }]
        };
        _productBundleRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(bundle);
        _priceEntryRepository
            .Setup(r => r.GetLatestForStoreAsync(1, StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceEntry { ProductId = 1, StoreId = StoreId, Price = new Money(5, "TJS") });
        _stockLevelRepository.Setup(r => r.TryDecrementAsync(1, StoreId, 6, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();

        // 3 bundles x 2 units of product 1 each = 6 total units decremented; total price = 10 * 3 = 30.
        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-bundle4", "TJS", [], BundleLines: [new ProcessSaleBundleLine(1, 3)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.Completed, result.Outcome);
        Assert.Equal(30m, result.TotalAmount);
        _stockLevelRepository.Verify(r => r.TryDecrementAsync(1, StoreId, 6, It.IsAny<CancellationToken>()), Times.Once);
    }
}
