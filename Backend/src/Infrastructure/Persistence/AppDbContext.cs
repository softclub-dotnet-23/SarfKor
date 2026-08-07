using Domain.Analytics;
using Domain.Assistant;
using Domain.Auditing;
using Domain.Catalog;
using Domain.Customers;
using Domain.Engagement;
using Domain.Feedback;
using Domain.Identity;
using Domain.Inventory;
using Domain.Loyalty;
using Domain.Notifications;
using Domain.Offers;
using Domain.Payments;
using Domain.Pricing;
using Domain.Products;
using Domain.Receipts;
using Domain.Reputation;
using Domain.Sales;
using Domain.Security;
using Domain.ShoppingLists;
using Domain.Stores;
using Domain.Subscriptions;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Scan> Scans => Set<Scan>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<PendingAssistantAction> PendingAssistantActions => Set<PendingAssistantAction>();

    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductBundle> ProductBundles => Set<ProductBundle>();
    public DbSet<ProductBundleItem> ProductBundleItems => Set<ProductBundleItem>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Favorite> Favorites => Set<Favorite>();

    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewReply> ReviewReplies => Set<ReviewReply>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserConsent> UserConsents => Set<UserConsent>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<AdminInvitation> AdminInvitations => Set<AdminInvitation>();

    public DbSet<CostPrice> CostPrices => Set<CostPrice>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLineItem> PurchaseOrderLineItems => Set<PurchaseOrderLineItem>();
    public DbSet<ReorderRule> ReorderRules => Set<ReorderRule>();
    public DbSet<StockLevel> StockLevels => Set<StockLevel>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();
    public DbSet<LoyaltyProgram> LoyaltyPrograms => Set<LoyaltyProgram>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();

    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();

    public DbSet<ExpiringOffer> ExpiringOffers => Set<ExpiringOffer>();
    public DbSet<Promotion> Promotions => Set<Promotion>();

    public DbSet<GiftCard> GiftCards => Set<GiftCard>();
    public DbSet<GiftCardRedemption> GiftCardRedemptions => Set<GiftCardRedemption>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<StoreCredit> StoreCredits => Set<StoreCredit>();

    public DbSet<PriceEntry> PriceEntries => Set<PriceEntry>();

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSubmission> ProductSubmissions => Set<ProductSubmission>();

    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLineItem> ReceiptLineItems => Set<ReceiptLineItem>();

    public DbSet<ContributorTrustScore> ContributorTrustScores => Set<ContributorTrustScore>();
    public DbSet<ContributorTrustScoreAdjustment> ContributorTrustScoreAdjustments => Set<ContributorTrustScoreAdjustment>();

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<StoreSubscription> StoreSubscriptions => Set<StoreSubscription>();
    public DbSet<SubscriptionPayment> SubscriptionPayments => Set<SubscriptionPayment>();

    public DbSet<CashierShift> CashierShifts => Set<CashierShift>();
    public DbSet<Commission> Commissions => Set<Commission>();
    public DbSet<FiscalReceipt> FiscalReceipts => Set<FiscalReceipt>();
    public DbSet<ReturnLineItem> ReturnLineItems => Set<ReturnLineItem>();
    public DbSet<SaleLineItem> SaleLineItems => Set<SaleLineItem>();
    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
    public DbSet<SaleTransaction> SaleTransactions => Set<SaleTransaction>();

    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();

    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();

    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StoreEmployee> StoreEmployees => Set<StoreEmployee>();
    public DbSet<StoreEmployeeInvitation> StoreEmployeeInvitations => Set<StoreEmployeeInvitation>();
    public DbSet<StoreOwnerInvitation> StoreOwnerInvitations => Set<StoreOwnerInvitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Backs ProductConfiguration's trigram index — plain btree can't accelerate a `%term%`
        // substring ILIKE, gin_trgm_ops can. Needed once per database.
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
