using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Application;
using Application.Sales.Commands.ProcessSale;
using Infrastructure;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using WebApi;
using WebApi.Swagger;

// Structured logging (CLAUDE.md §10: "нужен дашборд состояния системы") — the file sink is
// JSON because a log aggregator (Grafana Loki, ELK, etc.) needs that shape to build a dashboard
// on; the console sink is plain text because a human watches that one directly in a terminal.
// EF Core's per-query "Executed DbCommand" logging is dropped to Warning on both — it's the
// single noisiest category by far and isn't useful outside active query debugging.
// Configured before CreateBuilder so startup failures (bad connection string, etc.) are logged
// too, not just requests handled after the host is already up.
const string consoleTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: consoleTemplate)
    .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(), "logs/sarfkor-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: consoleTemplate)
    .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(), "logs/sarfkor-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

// Fail fast with a clear message at startup rather than a confusing NullReferenceException the
// first time a request tries to sign/validate a JWT — a missing env var in a fresh deploy should
// be obvious from the logs immediately, not discovered on the first login attempt.
foreach (var key in new[] { "Jwt:Issuer", "Jwt:Audience", "Jwt:Key" })
{
    if (string.IsNullOrWhiteSpace(builder.Configuration[key]))
        throw new InvalidOperationException($"Configuration key '{key}' is not set — set it via User Secrets (dev) or the {key.Replace(":", "__")} environment variable (prod).");
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<Application.Assistant.AssistantOptions>(builder.Configuration.GetSection(Application.Assistant.AssistantOptions.SectionName));

// Enums serialize/deserialize as their string name ("Product", "Android") instead of a raw
// integer — matters for every request/response DTO that carries an enum field, so it belongs
// globally. MVC controllers and Minimal APIs read JSON options from two different places
// (Mvc.JsonOptions vs Http.Json.JsonOptions) — both must be configured, or enum fields bound
// through [FromBody] on a controller silently fall back to raw integers in both directions.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

// No handler was registered before this, so an uncaught exception (e.g. a raw DbUpdateException
// from a missing FK check) fell through to the framework's default behavior — an inconsistent,
// unstructured response shape compared to the app's own ProblemDetails-style validation errors,
// and a risk of leaking internals depending on hosting config. This logs full details server-side
// via the existing Serilog pipeline but returns only a generic, safe body to the client.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Backs AdminMetricsController's short-lived summary cache (ADMIN_PROMPT.md §2.5).
builder.Services.AddMemoryCache();
builder.Services.Configure<Application.Subscriptions.SubscriptionOptions>(
    builder.Configuration.GetSection(Application.Subscriptions.SubscriptionOptions.SectionName));
builder.Services.Configure<Application.Stores.StoreEmployeeInvitationOptions>(
    builder.Configuration.GetSection(Application.Stores.StoreEmployeeInvitationOptions.SectionName));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateLifetime = true
        };
        // ADMIN_PROMPT.md §2.3: "не полагайся только на проверку при логине" — an Admin block must
        // take effect immediately, not just block future logins/refreshes. This runs on every
        // authenticated request (one extra indexed PK lookup) and rejects a still-unexpired access
        // token the moment ApplicationUser.BlockedAt is set, closing the up-to-15-minute gap a
        // block-only-at-login/refresh check would otherwise leave open.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId is null) return;

                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByIdAsync(userId);
                if (user?.BlockedAt is not null)
                    context.Fail("This account has been blocked.");
            }
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("StorePartner", policy => policy.RequireRole("StorePartner"))
    .AddPolicy("Admin", policy => policy.RequireRole("Admin"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Сканирование/сравнение — публичные, читаемые часто мобильным приложением.
    options.AddPolicy("scan", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));

    // Регистрация — против спам-аккаунтов.
    options.AddPolicy("registration", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromHours(1) }));

    // Вход — против перебора паролей.
    options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(15) }));

    // Забыли пароль / сброс — против спама на чужой email и перебора токена.
    options.AddPolicy("password-reset", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromHours(1) }));

    // Принятие приглашения кассира и публичная проверка токена — неаутентифицированные, создают
    // аккаунт / открывают контекст приглашения, токен угадываем по перебору.
    options.AddPolicy("invite-accept", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromHours(1) }));

    // Повторная отправка письма-приглашения кассиру — аутентифицированное действие владельца, но
    // само шлёт письмо, поэтому отдельный, более жёсткий лимит, чем обычные partner-write.
    options.AddPolicy("invite-resend", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromHours(1) }));

    // Пользовательский контент (цены, жалобы, сверка чека) — против спама/накрутки.
    options.AddPolicy("contributions", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromHours(1) }));

    // ProcessSaleCommand — кассир может пробивать много чеков подряд, лимит щедрее.
    options.AddPolicy("sales", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));

    // Обычные аутентифицированные операции StorePartner (каталог, склад, поставщики,
    // отзывы...) — щедрый лимит просто как страховка от automation/ошибок клиента, не от
    // конкретной угрозы.
    options.AddPolicy("partner-write", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1) }));

    // Операции, двигающие реальные деньги вне обычного чека (выпуск/погашение подарочных
    // карт и store credit, отмена продажи) — плотнее, чем обычные операции.
    options.AddPolicy("money-write", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));

    // ИИ-ассистент — каждое обращение стоит денег (вызов LLM API), а окно чата легко
    // заспамить сообщениями; отдельная, более жёсткая политика, чем обычные операции.
    options.AddPolicy("assistant", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 15, Window = TimeSpan.FromMinutes(5) }));

    // Смена пароля / загрузка аватара — аутентифицированные, но чувствительные операции
    // (перебор текущего пароля, спам загрузок на диск); партиционируется по пользователю,
    // а не по IP, так как обе операции уже требуют валидного JWT.
    options.AddPolicy("account-security", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromHours(1) }));
});

var app = builder.Build();

app.UseSerilogRequestLogging();

using (var scope = app.Services.CreateScope())
{
    // Applies any pending EF Core migrations before anything below touches the database — a fresh
    // environment (e.g. a new Railway deploy against an empty Postgres) has no tables at all yet,
    // so RoleManager.RoleExistsAsync would otherwise fail with "relation does not exist" before the
    // app ever serves a request. Idempotent: a no-op if every migration is already applied.
    var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    migrationLogger.LogInformation("Applying database migrations...");
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    migrationLogger.LogInformation("Database migrations applied successfully.");

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "User", "StorePartner", "Admin" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Bootstraps the first Admin account from configuration (Admin:Email / Admin:Password —
    // user-secrets in dev, environment variables in prod per CLAUDE.md §2, never hardcoded here).
    // There is no public endpoint that grants the Admin role, so without this seeder the
    // moderation/admin surface would be permanently unreachable on a fresh database.
    var adminEmail = builder.Configuration["Admin:Email"];
    var adminPassword = builder.Configuration["Admin:Password"];
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
    {
        logger.LogWarning("Admin:Email / Admin:Password are not configured — skipping admin account seeding.");
    }
    else
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser is null)
        {
            adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, CreatedAt = DateTimeOffset.UtcNow };
            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                logger.LogError(
                    "Failed to seed admin account {AdminEmail}: {Errors}",
                    adminEmail, string.Join("; ", createResult.Errors.Select(e => e.Description)));
                adminUser = null;
            }
        }

        if (adminUser is not null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
            await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Sarfkor API v1"));

    // SmtpEmailSender already handles a missing Smtp:Username/Smtp:Password by logging the email
    // instead of sending it — deliberately, so OTP flows (register/forgot-password) stay testable
    // without real mail infrastructure. But that per-email log line is easy to miss, and every
    // endpoint still returns success either way (ForgotPasswordCommandHandler's anti-enumeration
    // guarantee — see its own comment — must hold even when SMTP is broken), so nothing in the API
    // response can tell a developer "this is only pretending to send." This banner fires once at
    // startup instead, before anyone has to trigger a register/reset and go grep the log to find
    // out. Development-only on purpose: Production must stay silent about this exact configuration
    // detail the same way it stays silent about whether any given email is registered.
    if (string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Username"]) || string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Password"]))
    {
        app.Services.GetRequiredService<ILogger<Program>>().LogWarning(
            "=== SMTP NOT CONFIGURED === Registration and password-reset emails will be logged to " +
            "this console instead of actually sent. Run `dotnet user-secrets set Smtp:Username ...` " +
            "and `dotnet user-secrets set Smtp:Password ...` from Backend/src/WebApi to send real email.");
    }
}
else
{
    // CLAUDE.md §2: "HTTPS everywhere, HSTS в проде" — browsers cache this and refuse plain-HTTP
    // on repeat visits, closing the SSL-stripping window that HTTPS redirection alone leaves open.
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();

public sealed record RecordScanRequest(int ProductId, int? StoreId);
public sealed record CreatePromotionRequest(
    int StoreId,
    int? ProductId,
    int? CategoryId,
    Domain.Offers.PromotionDiscountType DiscountType,
    decimal DiscountValue,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);
public sealed record RecordCommissionRequest(decimal Amount, string Currency);
public sealed record LoginRequest(string Email, string Password);
public sealed record UpdateUserProfileRequest(string DisplayName, string? AvatarReference, string PreferredLanguage);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record RecordUserConsentRequest(Domain.Identity.ConsentType Type, bool IsGranted);
public sealed record CreateStoreEmployeeInvitationRequest(string Email, Domain.Stores.StoreEmployeeRole Role);
public sealed record ConfirmAdminInvitationRequest(string Email, string Code, string Password);
public sealed record OpenCashierShiftRequest(int StoreId, decimal OpeningCash, string Currency);
public sealed record CloseCashierShiftRequest(decimal ClosingCash);
public sealed record ProcessReturnRequest(IReadOnlyList<Application.Sales.Commands.ProcessReturn.ProcessReturnLineInput> Lines, string Reason);
public sealed record CreatePurchaseOrderRequest(int StoreId, int SupplierId, IReadOnlyList<Application.Inventory.Commands.CreatePurchaseOrder.CreatePurchaseOrderLineInput> Lines);
public sealed record InitiateStockTransferRequest(int ProductId, int FromStoreId, int ToStoreId, int Quantity);
public sealed record CreateReorderRuleRequest(int StoreId, int ProductId, int ThresholdQuantity, int ReorderQuantity, int? PreferredSupplierId);
public sealed record CreateProductBundleRequest(int StoreId, string Name, decimal BundlePrice, string Currency, IReadOnlyList<Application.Catalog.Commands.CreateProductBundle.CreateProductBundleItemInput> Items);
public sealed record CreateCustomerRequest(string PhoneNumber, string? FullName);
public sealed record CreateLoyaltyProgramRequest(int StoreId, decimal PointsPerCurrencyUnit, decimal RedemptionRate);
public sealed record EnrollCustomerInLoyaltyRequest(int CustomerId, int LoyaltyProgramId);
public sealed record EarnLoyaltyPointsRequest(int Points, int? SaleTransactionId);
public sealed record RedeemLoyaltyPointsRequest(int Points);
public sealed record IssueGiftCardRequest(int StoreId, decimal Amount, string Currency, DateTimeOffset? ExpiresAt);
public sealed record RedeemGiftCardRequest(int StoreId, decimal Amount, string Currency);
public sealed record IssueStoreCreditRequest(int StoreId, int CustomerId, decimal Amount, string Currency);
public sealed record RedeemStoreCreditRequest(int StoreId, int CustomerId, decimal Amount, string Currency);
public sealed record CreateShoppingListRequest(string Name);
public sealed record AddShoppingListItemRequest(int ProductId, int Quantity);
public sealed record FavoriteRequest(Domain.Engagement.FavoriteType Type, int EntityId);
public sealed record SubmitReviewRequest(int? StoreId, int Rating, string Comment);
public sealed record ReplyToReviewRequest(string Message);
public sealed record CreatePriceAlertRequest(int ProductId, decimal TargetPrice, string Currency);
public sealed record RegisterDeviceTokenRequest(string Token, Domain.Notifications.DevicePlatform Platform);
public sealed record CreateStoreRequest(string Name, string Address, double Latitude, double Longitude);
public sealed record AdminCreateStorePartnerRequest(string Email, string StoreName, string Address, double Latitude, double Longitude);
public sealed record UpdateStoreEmployeeRequest(decimal? MonthlySalaryAmount, string? MonthlySalaryCurrency, TimeOnly? ScheduleStart, TimeOnly? ScheduleEnd);
public sealed record SetCostPriceRequest(int StoreId, int ProductId, decimal Amount, string Currency);
public sealed record SubmitPriceUpdateRequest(int ProductId, int StoreId, decimal Price, string Currency);
public sealed record ProcessSaleRequest(
    int StoreId,
    string IdempotencyKey,
    string Currency,
    IReadOnlyList<ProcessSaleLine> Lines,
    string? GiftCardCode = null,
    int? CustomerId = null,
    bool ApplyStoreCredit = false,
    IReadOnlyList<ProcessSaleBundleLine>? BundleLines = null);
public sealed record VoidSaleRequest(string Reason);
public sealed record RecordStockReceiptRequest(int StoreId, int ProductId, int Quantity, int? SupplierId);
public sealed record ReportOutOfStockRequest(int ProductId, int? StoreId, string Description);
public sealed record PublishExpiringOfferRequest(int StoreId, int ProductId, decimal OriginalPrice, decimal DiscountedPrice, string Currency, DateTimeOffset ExpiresAt);
public sealed record SubmitNewProductRequest(string Barcode, string Name, int CategoryId, int BrandId, string CountryOfOrigin);
public sealed record UpdateBrandRequest(string Name);
public sealed record UpdateCategoryRequest(string Name, int? ParentCategoryId, int DisplayOrder, bool IsHidden);
public sealed record CreateTaxRateRequest(string Name, decimal Percentage, int? CategoryId, DateOnly? EffectiveFrom, DateOnly? EffectiveTo);
public sealed record UpdateTaxRateRequest(string Name, decimal Percentage, int? CategoryId, DateOnly? EffectiveFrom, DateOnly? EffectiveTo);
public sealed record MergeBrandsRequest(int TargetBrandId, IReadOnlyList<int> SourceBrandIds);
public sealed record ChangeStoreStatusRequest(Domain.Stores.StoreStatus NewStatus, string Reason);
public sealed record UpdateStoreTaxSettingsRequest(bool IsVatPayer, Domain.Stores.StoreTaxRegime TaxRegime);
public sealed record InviteAdminRequest(string Email);
public sealed record BlockUserRequest(string Reason);
public sealed record UnblockUserRequest(string Reason);
public sealed record AdjustTrustScoreRequest(double Delta, string Reason);
public sealed record CreateSubscriptionPlanRequest(
    string Name, string Code, decimal MonthlyPriceAmount, string MonthlyPriceCurrency,
    int? MaxStores, int? MaxEmployees, IReadOnlyList<string>? Features);
public sealed record UpdateSubscriptionPlanRequest(
    string Name, decimal MonthlyPriceAmount, string MonthlyPriceCurrency,
    int? MaxStores, int? MaxEmployees, IReadOnlyList<string>? Features, bool IsActive);
public sealed record ChangeStoreSubscriptionPlanRequest(int NewSubscriptionPlanId);
public sealed record CancelStoreSubscriptionRequest(string Reason);
public sealed record RecordSubscriptionPaymentRequest(
    decimal Amount, string Currency, DateOnly PeriodStart, DateOnly PeriodEnd,
    Domain.Subscriptions.SubscriptionPaymentMethod Method, string? Comment);
public sealed record ReverseSubscriptionPaymentRequest(string Reason);
public sealed record CreateSupplierRequest(int StoreId, string Name, string? ContactPhone, string? ContactEmail);
public sealed record UpdateSupplierRequest(string Name, string? ContactPhone, string? ContactEmail);
public sealed record AssistantChatMessageRequest(string Role, string Content);
public sealed record AssistantChatRequest(int? StoreId, IReadOnlyList<AssistantChatMessageRequest> History, string Message);
