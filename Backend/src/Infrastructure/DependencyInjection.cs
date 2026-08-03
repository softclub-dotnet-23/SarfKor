using Application.Abstractions;
using Application.Assistant.Abstractions;
using Infrastructure.Assistant;
using Infrastructure.Email;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // EnableRetryOnFailure absorbs the transient connection-pool-exhaustion/timeout errors seen
        // right after a fresh restart under concurrent load (several requests opening brand-new
        // pooled connections at once) instead of surfacing them as a 500 to the client.
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                // Explicit, not relying on library defaults — caps password-guessing per account
                // regardless of source IP, on top of (not instead of) the "login" rate-limit policy.
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // Default is 1 day — a password-reset link sitting in an inbox doesn't need that long a window.
        services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(1));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IPriceEntryRepository, PriceEntryRepository>();
        services.AddScoped<IContributorTrustScoreRepository, ContributorTrustScoreRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ISaleTransactionRepository, SaleTransactionRepository>();
        services.AddScoped<IStockLevelRepository, StockLevelRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<ICostPriceRepository, CostPriceRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReceiptRepository, ReceiptRepository>();
        services.AddScoped<IExpiringOfferRepository, ExpiringOfferRepository>();
        services.AddScoped<IProductSubmissionRepository, ProductSubmissionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITaxRateRepository, TaxRateRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewReplyRepository, ReviewReplyRepository>();
        services.AddScoped<IPriceAlertRepository, PriceAlertRepository>();
        services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ILoyaltyProgramRepository, LoyaltyProgramRepository>();
        services.AddScoped<ILoyaltyAccountRepository, LoyaltyAccountRepository>();
        services.AddScoped<ILoyaltyTransactionRepository, LoyaltyTransactionRepository>();
        services.AddScoped<IGiftCardRepository, GiftCardRepository>();
        services.AddScoped<IGiftCardRedemptionRepository, GiftCardRedemptionRepository>();
        services.AddScoped<IStoreCreditRepository, StoreCreditRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IStockTransferRepository, StockTransferRepository>();
        services.AddScoped<IReorderRuleRepository, ReorderRuleRepository>();
        services.AddScoped<IProductBundleRepository, ProductBundleRepository>();
        services.AddScoped<ICashierShiftRepository, CashierShiftRepository>();
        services.AddScoped<ISaleReturnRepository, SaleReturnRepository>();
        services.AddScoped<IPriceEntryDisputeRepository, PriceEntryDisputeRepository>();
        services.AddScoped<IReportDisputeRepository, ReportDisputeRepository>();
        services.AddScoped<ISecurityEventRepository, SecurityEventRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IUserConsentRepository, UserConsentRepository>();
        services.AddScoped<IStoreEmployeeRepository, StoreEmployeeRepository>();
        services.AddScoped<IStoreEmployeeInvitationRepository, StoreEmployeeInvitationRepository>();
        services.AddScoped<IStoreOwnerInvitationRepository, StoreOwnerInvitationRepository>();
        services.AddScoped<IScanRepository, ScanRepository>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<ICommissionRepository, CommissionRepository>();
        services.AddScoped<IPendingAssistantActionRepository, PendingAssistantActionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.Configure<AnthropicOptions>(configuration.GetSection(AnthropicOptions.SectionName));
        // Decided once at startup, not per-call (unlike SmtpEmailSender's per-send check) -- there's
        // no per-request "degrade gracefully" story for a chat endpoint the way there is for "log the
        // email instead of sending it", so which client type answers every request is fixed for the
        // process's lifetime by whether a key was configured when it started.
        var anthropicApiKey = configuration[$"{AnthropicOptions.SectionName}:ApiKey"];
        if (string.IsNullOrWhiteSpace(anthropicApiKey))
        {
            services.AddScoped<IAssistantChatClient, StubAssistantChatClient>();
        }
        else
        {
            services.AddHttpClient<IAssistantChatClient, AnthropicAssistantChatClient>((sp, client) =>
            {
                var anthropicOptions = sp.GetRequiredService<IOptions<AnthropicOptions>>().Value;
                client.BaseAddress = new Uri(anthropicOptions.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(60);
            });
        }

        return services;
    }
}
