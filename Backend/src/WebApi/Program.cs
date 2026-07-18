using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Application;
using Application.Catalog.Commands.CreateBrand;
using Application.Catalog.Commands.CreateCategory;
using Application.Catalog.Commands.CreateProductBundle;
using Application.Catalog.Commands.CreateTaxRate;
using Application.Catalog.Queries.GetBrands;
using Application.Catalog.Queries.GetCategories;
using Application.Catalog.Queries.GetProductBundles;
using Application.Catalog.Queries.GetTaxRates;
using Application.Common;
using Application.Customers.Commands.CreateCustomer;
using Application.Customers.Queries.GetCustomerByPhone;
using Application.Engagement.Commands.AddFavorite;
using Application.Engagement.Commands.RemoveFavorite;
using Application.Engagement.Queries.GetFavorites;
using Application.Feedback.Commands.ModerateReport;
using Application.Feedback.Commands.ReplyToReview;
using Application.Feedback.Commands.ReportOutOfStock;
using Application.Feedback.Commands.SubmitReview;
using Application.Feedback.Queries.GetReviews;
using Application.Identity.Commands.Login;
using Application.Identity.Commands.RefreshToken;
using Application.Identity.Commands.Register;
using Application.Inventory.Commands.CompleteStockTransfer;
using Application.Inventory.Commands.CreatePurchaseOrder;
using Application.Inventory.Commands.CreateReorderRule;
using Application.Inventory.Commands.CreateSupplier;
using Application.Inventory.Commands.InitiateStockTransfer;
using Application.Inventory.Commands.ReceivePurchaseOrder;
using Application.Inventory.Commands.RecordStockReceipt;
using Application.Inventory.Commands.SetCostPrice;
using Application.Inventory.Commands.SubmitPurchaseOrder;
using Application.Inventory.Queries.GetPurchaseOrders;
using Application.Inventory.Queries.GetReorderAlerts;
using Application.Inventory.Queries.GetStockLevel;
using Application.Inventory.Queries.GetStockTransfers;
using Application.Inventory.Queries.GetSuppliers;
using Application.Loyalty.Commands.CreateLoyaltyProgram;
using Application.Loyalty.Commands.EarnLoyaltyPoints;
using Application.Loyalty.Commands.EnrollCustomerInLoyalty;
using Application.Loyalty.Commands.RedeemLoyaltyPoints;
using Application.Loyalty.Queries.GetLoyaltyAccount;
using Application.Loyalty.Queries.GetLoyaltyProgram;
using Application.Notifications.Commands.CreatePriceAlert;
using Application.Notifications.Commands.DeactivatePriceAlert;
using Application.Notifications.Commands.MarkNotificationAsRead;
using Application.Notifications.Commands.RegisterDeviceToken;
using Application.Notifications.Queries.GetNotifications;
using Application.Notifications.Queries.GetPriceAlerts;
using Application.Offers.Commands.PublishExpiringOffer;
using Application.Offers.Queries.GetExpiringOffers;
using Application.Payments.Commands.IssueGiftCard;
using Application.Payments.Commands.IssueStoreCredit;
using Application.Payments.Commands.RedeemGiftCard;
using Application.Payments.Commands.RedeemStoreCredit;
using Application.Payments.Queries.GetGiftCardBalance;
using Application.Payments.Queries.GetStoreCreditBalance;
using Application.Pricing.Commands.SubmitPriceUpdate;
using Application.Products.Commands.ModerateNewProduct;
using Application.Products.Queries.CompareStoresForShoppingList;
using Application.Products.Queries.GetTopSellingProducts;
using Application.Products.Queries.ScanBarcode;
using Application.Receipts.Commands.UploadReceipt;
using Application.Receipts.Commands.VerifyReceipt;
using Application.Sales.Commands.CloseCashierShift;
using Application.Sales.Commands.OpenCashierShift;
using Application.Sales.Commands.ProcessReturn;
using Application.Sales.Commands.ProcessSale;
using Application.Sales.Commands.VoidSale;
using Application.Sales.Queries.GetCashierAnomalyReport;
using Application.Sales.Queries.GetCashierShifts;
using Application.Sales.Queries.GetDailySalesReport;
using Application.Sales.Queries.GetProfitReport;
using Application.Sales.Queries.GetReturnsForSale;
using Application.ShoppingLists.Commands.AddShoppingListItem;
using Application.ShoppingLists.Commands.CreateShoppingList;
using Application.ShoppingLists.Commands.RemoveShoppingListItem;
using Application.ShoppingLists.Queries.GetShoppingLists;
using Application.Stores.Commands.CreateStore;
using Application.Stores.Queries.GetStoreDashboard;
using FluentValidation;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

// Enums serialize/deserialize as their string name ("Product", "Android") instead of a raw
// integer — matters for every request DTO that carries an enum field, so it belongs globally.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

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

    // Пользовательский контент (цены, жалобы, сверка чека) — против спама/накрутки.
    options.AddPolicy("contributions", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromHours(1) }));

    // ProcessSaleCommand — кассир может пробивать много чеков подряд, лимит щедрее.
    options.AddPolicy("sales", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "User", "StorePartner", "Admin" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // CLAUDE.md §2: "HTTPS everywhere, HSTS в проде" — browsers cache this and refuse plain-HTTP
    // on repeat visits, closing the SSL-stripping window that HTTPS redirection alone leaves open.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Foundation for external monitoring (CLAUDE.md §10: "нужен дашборд состояния системы") — an
// uptime check or Grafana/Prometheus scraper polls this; deliberately unauthenticated (that's
// standard for health probes) and returns only a status/timestamp, never internal details.
app.MapGet("/health", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow })
            : Results.Json(new { status = "unhealthy", timestamp = DateTimeOffset.UtcNow }, statusCode: 503);
    }
    catch
    {
        return Results.Json(new { status = "unhealthy", timestamp = DateTimeOffset.UtcNow }, statusCode: 503);
    }
})
.WithName("HealthCheck");

app.MapPost("/api/auth/register", async (
    RegisterCommand command,
    ICommandHandler<RegisterCommand, Application.Abstractions.AuthResult?> handler,
    IValidator<RegisterCommand> validator,
    CancellationToken cancellationToken) =>
{
    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result is null ? Results.BadRequest("Registration failed.") : Results.Ok(result);
})
.RequireRateLimiting("registration")
.WithName("Register");

app.MapPost("/api/auth/login", async (
    LoginCommand command,
    ICommandHandler<LoginCommand, Application.Abstractions.AuthResult?> handler,
    IValidator<LoginCommand> validator,
    CancellationToken cancellationToken) =>
{
    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
})
.RequireRateLimiting("login")
.WithName("Login");

app.MapPost("/api/auth/refresh", async (
    RefreshTokenCommand command,
    ICommandHandler<RefreshTokenCommand, Application.Abstractions.AuthResult?> handler,
    IValidator<RefreshTokenCommand> validator,
    CancellationToken cancellationToken) =>
{
    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
})
.RequireRateLimiting("login")
.WithName("RefreshToken");

app.MapPost("/api/stores", async (
    CreateStoreRequest request,
    ClaimsPrincipal user,
    ICommandHandler<CreateStoreCommand, CreateStoreResult> handler,
    IValidator<CreateStoreCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new CreateStoreCommand(userId, request.Name, request.Address, request.Latitude, request.Longitude);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return Results.Ok(result);
})
.RequireAuthorization()
.RequireRateLimiting("contributions")
.WithName("CreateStore");

app.MapGet("/api/products/scan/{barcode}", async (
    string barcode,
    double? lat,
    double? lng,
    IQueryHandler<ScanBarcodeQuery, ScanBarcodeResult?> handler,
    IValidator<ScanBarcodeQuery> validator,
    CancellationToken cancellationToken) =>
{
    var query = new ScanBarcodeQuery(barcode, lat, lng);

    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(query, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
})
.RequireRateLimiting("scan")
.WithName("ScanBarcode");

app.MapPost("/api/prices", async (
    SubmitPriceUpdateRequest request,
    ClaimsPrincipal user,
    ICommandHandler<SubmitPriceUpdateCommand, SubmitPriceUpdateResult?> handler,
    IValidator<SubmitPriceUpdateCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new SubmitPriceUpdateCommand(request.ProductId, request.StoreId, userId, request.Price, request.Currency);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
})
.RequireAuthorization()
.RequireRateLimiting("contributions")
.WithName("SubmitPriceUpdate");

app.MapPost("/api/sales", async (
    ProcessSaleRequest request,
    ClaimsPrincipal user,
    ICommandHandler<ProcessSaleCommand, ProcessSaleResult> handler,
    IValidator<ProcessSaleCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new ProcessSaleCommand(request.StoreId, userId, request.IdempotencyKey, request.Currency, request.Lines);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        ProcessSaleOutcome.Completed => Results.Ok(result),
        ProcessSaleOutcome.StoreNotFound => Results.NotFound("Store not found."),
        ProcessSaleOutcome.Forbidden => Results.Forbid(),
        ProcessSaleOutcome.ProductNotFound => Results.NotFound($"Product {result.FailedProductId} not found."),
        ProcessSaleOutcome.PriceNotFound => Results.Conflict($"No price set for product {result.FailedProductId} at this store."),
        ProcessSaleOutcome.InsufficientStock => Results.Conflict($"Insufficient stock for product {result.FailedProductId}."),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.RequireRateLimiting("sales")
.WithName("ProcessSale");

app.MapPost("/api/sales/{id:int}/void", async (
    int id,
    VoidSaleRequest request,
    ClaimsPrincipal user,
    ICommandHandler<VoidSaleCommand, VoidSaleResult> handler,
    IValidator<VoidSaleCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new VoidSaleCommand(id, userId, request.Reason);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        VoidSaleOutcome.Voided => Results.Ok(result),
        VoidSaleOutcome.NotFound => Results.NotFound(),
        VoidSaleOutcome.Forbidden => Results.Forbid(),
        VoidSaleOutcome.AlreadyVoided => Results.Conflict("This sale has already been voided."),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("VoidSale");

// Cashier shifts and partial returns. No separate "cashier" sub-role exists yet (CLAUDE.md §9
// open question) — for now the store owner opens/closes their own shifts and processes returns.

app.MapPost("/api/cashier-shifts/open", async (
    OpenCashierShiftRequest request,
    ClaimsPrincipal user,
    ICommandHandler<OpenCashierShiftCommand, OpenCashierShiftResult> handler,
    IValidator<OpenCashierShiftCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new OpenCashierShiftCommand(request.StoreId, request.OpeningCash, request.Currency, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        OpenCashierShiftOutcome.Opened => Results.Ok(result),
        OpenCashierShiftOutcome.StoreNotFound => Results.NotFound("Store not found."),
        OpenCashierShiftOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("OpenCashierShift");

app.MapPost("/api/cashier-shifts/{cashierShiftId:int}/close", async (
    int cashierShiftId,
    CloseCashierShiftRequest request,
    ClaimsPrincipal user,
    ICommandHandler<CloseCashierShiftCommand, CloseCashierShiftResult> handler,
    IValidator<CloseCashierShiftCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new CloseCashierShiftCommand(cashierShiftId, request.ClosingCash, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        CloseCashierShiftOutcome.Closed => Results.Ok(result),
        CloseCashierShiftOutcome.NotFound => Results.NotFound(),
        CloseCashierShiftOutcome.Forbidden => Results.Forbid(),
        CloseCashierShiftOutcome.AlreadyClosed => Results.Conflict("This shift has already been closed."),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("CloseCashierShift");

app.MapGet("/api/stores/{storeId:int}/cashier-shifts", async (
    int storeId,
    ClaimsPrincipal user,
    IQueryHandler<GetCashierShiftsQuery, GetCashierShiftsResult> handler,
    IValidator<GetCashierShiftsQuery> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var query = new GetCashierShiftsQuery(storeId, userId);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(query, cancellationToken);
    return result.Outcome switch
    {
        GetCashierShiftsOutcome.Found => Results.Ok(result),
        GetCashierShiftsOutcome.StoreNotFound => Results.NotFound(),
        GetCashierShiftsOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("GetCashierShifts");

app.MapPost("/api/sales/{saleTransactionId:int}/return", async (
    int saleTransactionId,
    ProcessReturnRequest request,
    ClaimsPrincipal user,
    ICommandHandler<ProcessReturnCommand, ProcessReturnResult> handler,
    IValidator<ProcessReturnCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new ProcessReturnCommand(saleTransactionId, request.Lines, request.Reason, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        ProcessReturnOutcome.Processed => Results.Ok(result),
        ProcessReturnOutcome.SaleNotFound => Results.NotFound("Sale not found."),
        ProcessReturnOutcome.Forbidden => Results.Forbid(),
        ProcessReturnOutcome.SaleNotCompleted => Results.Conflict("This sale is not completed."),
        ProcessReturnOutcome.LineNotFound => Results.NotFound("Sale line item not found."),
        ProcessReturnOutcome.ExceedsAvailableQuantity => Results.Conflict("Return quantity exceeds what's available for this line."),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("ProcessReturn");

app.MapGet("/api/sales/{saleTransactionId:int}/returns", async (
    int saleTransactionId,
    ClaimsPrincipal user,
    IQueryHandler<GetReturnsForSaleQuery, GetReturnsForSaleResult> handler,
    IValidator<GetReturnsForSaleQuery> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var query = new GetReturnsForSaleQuery(saleTransactionId, userId);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(query, cancellationToken);
    return result.Outcome switch
    {
        GetReturnsForSaleOutcome.Found => Results.Ok(result),
        GetReturnsForSaleOutcome.SaleNotFound => Results.NotFound(),
        GetReturnsForSaleOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("GetReturnsForSale");

app.MapPost("/api/stock/receipts", async (
    RecordStockReceiptRequest request,
    ClaimsPrincipal user,
    ICommandHandler<RecordStockReceiptCommand, RecordStockReceiptResult> handler,
    IValidator<RecordStockReceiptCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new RecordStockReceiptCommand(request.StoreId, request.ProductId, request.Quantity, userId, request.SupplierId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        RecordStockReceiptOutcome.Received => Results.Ok(result),
        RecordStockReceiptOutcome.StoreNotFound => Results.NotFound("Store not found."),
        RecordStockReceiptOutcome.ProductNotFound => Results.NotFound("Product not found."),
        RecordStockReceiptOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("RecordStockReceipt");

app.MapPost("/api/stock/cost-price", async (
    SetCostPriceRequest request,
    ClaimsPrincipal user,
    ICommandHandler<SetCostPriceCommand, SetCostPriceResult> handler,
    IValidator<SetCostPriceCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new SetCostPriceCommand(request.StoreId, request.ProductId, request.Amount, request.Currency, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        SetCostPriceOutcome.Set => Results.Ok(result),
        SetCostPriceOutcome.StoreNotFound => Results.NotFound("Store not found."),
        SetCostPriceOutcome.ProductNotFound => Results.NotFound("Product not found."),
        SetCostPriceOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("SetCostPrice");

app.MapGet("/api/stock", async (
    int storeId,
    ClaimsPrincipal user,
    IQueryHandler<GetStockLevelQuery, GetStockLevelResult> handler,
    IValidator<GetStockLevelQuery> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var query = new GetStockLevelQuery(storeId, userId);

    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(query, cancellationToken);
    return result.Outcome switch
    {
        GetStockLevelOutcome.Found => Results.Ok(result.Levels),
        GetStockLevelOutcome.StoreNotFound => Results.NotFound("Store not found."),
        GetStockLevelOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("GetStockLevel");

app.MapGet("/api/stores/{storeId:int}/dashboard", async (
    int storeId,
    ClaimsPrincipal user,
    IQueryHandler<GetStoreDashboardQuery, GetStoreDashboardResult> handler,
    IValidator<GetStoreDashboardQuery> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var query = new GetStoreDashboardQuery(storeId, userId);

    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(query, cancellationToken);
    return result.Outcome switch
    {
        GetStoreDashboardOutcome.Found => Results.Ok(result),
        GetStoreDashboardOutcome.StoreNotFound => Results.NotFound("Store not found."),
        GetStoreDashboardOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("GetStoreDashboard");

app.MapGet("/api/stores/{storeId:int}/reports/daily-sales", async (
    int storeId,
    DateOnly date,
    ClaimsPrincipal user,
    IQueryHandler<GetDailySalesReportQuery, GetDailySalesReportResult> handler,
    IValidator<GetDailySalesReportQuery> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var query = new GetDailySalesReportQuery(storeId, date, userId);

    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(query, cancellationToken);
    return result.Outcome switch
    {
        GetDailySalesReportOutcome.Found => Results.Ok(result),
        GetDailySalesReportOutcome.StoreNotFound => Results.NotFound("Store not found."),
        GetDailySalesReportOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("GetDailySalesReport");

app.MapGet("/api/stores/{storeId:int}/reports/profit", async (
    int storeId,
    DateOnly from,
    DateOnly to,
    ClaimsPrincipal user,
    IQueryHandler<GetProfitReportQuery, GetProfitReportResult> handler,
    IValidator<GetProfitReportQuery> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var query = new GetProfitReportQuery(storeId, from, to, userId);

    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(query, cancellationToken);
    return result.Outcome switch
    {
        GetProfitReportOutcome.Found => Results.Ok(result),
        GetProfitReportOutcome.StoreNotFound => Results.NotFound("Store not found."),
        GetProfitReportOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("GetProfitReport");

app.MapGet("/api/stores/{storeId:int}/reports/cashier-anomalies", async (
    int storeId,
    DateOnly from,
    DateOnly to,
    ClaimsPrincipal user,
    IQueryHandler<GetCashierAnomalyReportQuery, GetCashierAnomalyReportResult> handler,
    IValidator<GetCashierAnomalyReportQuery> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var query = new GetCashierAnomalyReportQuery(storeId, from, to, userId);

    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(query, cancellationToken);
    return result.Outcome switch
    {
        GetCashierAnomalyReportOutcome.Found => Results.Ok(result),
        GetCashierAnomalyReportOutcome.StoreNotFound => Results.NotFound("Store not found."),
        GetCashierAnomalyReportOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("GetCashierAnomalyReport");

app.MapGet("/api/products/top-selling", async (
    int? storeId,
    int? limit,
    IQueryHandler<GetTopSellingProductsQuery, GetTopSellingProductsResult> handler,
    IValidator<GetTopSellingProductsQuery> validator,
    CancellationToken cancellationToken) =>
{
    var query = new GetTopSellingProductsQuery(storeId, limit ?? 10);

    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(query, cancellationToken);
    return Results.Ok(result);
})
.WithName("GetTopSellingProducts");

app.MapGet("/api/products/compare-basket", async (
    int[] productIds,
    double? lat,
    double? lng,
    IQueryHandler<CompareStoresForShoppingListQuery, CompareStoresForShoppingListResult> handler,
    IValidator<CompareStoresForShoppingListQuery> validator,
    CancellationToken cancellationToken) =>
{
    var query = new CompareStoresForShoppingListQuery(productIds, lat, lng);

    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(query, cancellationToken);
    return Results.Ok(result);
})
.RequireRateLimiting("scan")
.WithName("CompareStoresForShoppingList");

app.MapPost("/api/reports/out-of-stock", async (
    ReportOutOfStockRequest request,
    ClaimsPrincipal user,
    ICommandHandler<ReportOutOfStockCommand, ReportOutOfStockResult> handler,
    IValidator<ReportOutOfStockCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new ReportOutOfStockCommand(userId, request.ProductId, request.StoreId, request.Description);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return Results.Ok(result);
})
.RequireAuthorization()
.RequireRateLimiting("contributions")
.WithName("ReportOutOfStock");

app.MapPost("/api/receipts/{id:int}/verify", async (
    int id,
    ClaimsPrincipal user,
    ICommandHandler<VerifyReceiptCommand, VerifyReceiptResult> handler,
    IValidator<VerifyReceiptCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new VerifyReceiptCommand(id, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        VerifyReceiptOutcome.Verified => Results.Ok(result),
        VerifyReceiptOutcome.Mismatched => Results.Ok(result),
        VerifyReceiptOutcome.NotFound => Results.NotFound(),
        VerifyReceiptOutcome.Forbidden => Results.Forbid(),
        VerifyReceiptOutcome.MissingStore => Results.Conflict("Receipt has no associated store."),
        VerifyReceiptOutcome.AlreadyProcessed => Results.Conflict("Receipt has already been processed."),
        _ => Results.Problem()
    };
})
.RequireAuthorization()
.RequireRateLimiting("contributions")
.WithName("VerifyReceipt");

app.MapPost("/api/receipts/upload", async (
    IFormFile file,
    [FromForm] int? storeId,
    [FromForm] string linesJson,
    ClaimsPrincipal user,
    IWebHostEnvironment env,
    IConfiguration configuration,
    ICommandHandler<UploadReceiptCommand, UploadReceiptResult> handler,
    IValidator<UploadReceiptCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    const long maxFileSizeBytes = 5 * 1024 * 1024;
    if (file.Length == 0 || file.Length > maxFileSizeBytes)
        return Results.BadRequest("File is empty or exceeds the 5 MB limit.");

    // Content-based check (magic bytes), never trust the client-supplied Content-Type header.
    var extension = await DetectImageExtensionAsync(file, cancellationToken);
    if (extension is null)
        return Results.BadRequest("Unsupported file type. Only JPEG and PNG images are accepted.");

    List<UploadReceiptLineInput>? lines;
    try
    {
        lines = JsonSerializer.Deserialize<List<UploadReceiptLineInput>>(
            linesJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    catch (JsonException)
    {
        return Results.BadRequest("Invalid line items payload.");
    }

    if (lines is null)
        return Results.BadRequest("Invalid line items payload.");

    // Stored under the content root, never under wwwroot, so it is never directly web-servable.
    var storageRoot = configuration["Storage:ReceiptsPath"] ?? Path.Combine(env.ContentRootPath, "App_Data", "receipts");
    Directory.CreateDirectory(storageRoot);

    // Filename is server-generated (never the client-supplied one) to rule out path traversal / overwrite attacks.
    var storedFileName = $"{Guid.NewGuid()}{extension}";
    await using (var destination = File.Create(Path.Combine(storageRoot, storedFileName)))
    {
        await file.CopyToAsync(destination, cancellationToken);
    }

    var command = new UploadReceiptCommand(userId, storeId, storedFileName, lines);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return Results.Ok(result);
})
.RequireAuthorization()
.RequireRateLimiting("contributions")
// This API is JWT Bearer, not cookie sessions — per CLAUDE.md §2, CSRF for JWT is mitigated by the
// strict CORS origin whitelist, not antiforgery tokens (which ASP.NET Core auto-requires for form binding).
.DisableAntiforgery()
.WithName("UploadReceipt");

app.MapPost("/api/offers", async (
    PublishExpiringOfferRequest request,
    ClaimsPrincipal user,
    ICommandHandler<PublishExpiringOfferCommand, PublishExpiringOfferResult> handler,
    IValidator<PublishExpiringOfferCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new PublishExpiringOfferCommand(
        request.StoreId, request.ProductId, request.OriginalPrice, request.DiscountedPrice, request.Currency, request.ExpiresAt, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        PublishExpiringOfferOutcome.Published => Results.Ok(result),
        PublishExpiringOfferOutcome.StoreNotFound => Results.NotFound("Store not found."),
        PublishExpiringOfferOutcome.ProductNotFound => Results.NotFound("Product not found."),
        PublishExpiringOfferOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("PublishExpiringOffer");

app.MapGet("/api/offers/expiring", async (
    int? storeId,
    double? lat,
    double? lng,
    IQueryHandler<GetExpiringOffersQuery, GetExpiringOffersResult> handler,
    IValidator<GetExpiringOffersQuery> validator,
    CancellationToken cancellationToken) =>
{
    var query = new GetExpiringOffersQuery(storeId, lat, lng);

    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(query, cancellationToken);
    return Results.Ok(result);
})
.RequireRateLimiting("scan")
.WithName("GetExpiringOffers");

app.MapPost("/api/admin/products/{submissionId:int}/moderate", async (
    int submissionId,
    ModerateNewProductRequest request,
    ClaimsPrincipal user,
    ICommandHandler<ModerateNewProductCommand, ModerateNewProductResult> handler,
    IValidator<ModerateNewProductCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new ModerateNewProductCommand(submissionId, request.Approve, userId, request.Reason);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        ModerateNewProductOutcome.Approved => Results.Ok(result),
        ModerateNewProductOutcome.Rejected => Results.Ok(result),
        ModerateNewProductOutcome.NotFound => Results.NotFound(),
        ModerateNewProductOutcome.AlreadyModerated => Results.Conflict("This submission has already been moderated."),
        _ => Results.Problem()
    };
})
.RequireAuthorization("Admin")
.WithName("ModerateNewProduct");

app.MapPost("/api/admin/reports/{reportId:int}/moderate", async (
    int reportId,
    ModerateReportRequest request,
    ClaimsPrincipal user,
    ICommandHandler<ModerateReportCommand, ModerateReportResult> handler,
    IValidator<ModerateReportCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new ModerateReportCommand(reportId, request.Resolve, userId, request.Reason);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        ModerateReportOutcome.Resolved => Results.Ok(result),
        ModerateReportOutcome.Rejected => Results.Ok(result),
        ModerateReportOutcome.NotFound => Results.NotFound(),
        ModerateReportOutcome.AlreadyModerated => Results.Conflict("This report has already been moderated."),
        _ => Results.Problem()
    };
})
.RequireAuthorization("Admin")
.WithName("ModerateReport");

// Consumer engagement: shopping lists, favorites, reviews, price alerts, push notifications.
// All scoped to the authenticated user (UserId from the JWT claim, never from the request body).

app.MapPost("/api/shopping-lists", async (
    CreateShoppingListRequest request,
    ClaimsPrincipal user,
    ICommandHandler<CreateShoppingListCommand, CreateShoppingListResult> handler,
    IValidator<CreateShoppingListCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new CreateShoppingListCommand(userId, request.Name);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(command, cancellationToken));
})
.RequireAuthorization()
.WithName("CreateShoppingList");

app.MapGet("/api/shopping-lists", async (
    ClaimsPrincipal user,
    IQueryHandler<GetShoppingListsQuery, GetShoppingListsResult> handler,
    IValidator<GetShoppingListsQuery> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var query = new GetShoppingListsQuery(userId);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(query, cancellationToken));
})
.RequireAuthorization()
.WithName("GetShoppingLists");

app.MapPost("/api/shopping-lists/{listId:int}/items", async (
    int listId,
    AddShoppingListItemRequest request,
    ClaimsPrincipal user,
    ICommandHandler<AddShoppingListItemCommand, AddShoppingListItemResult> handler,
    IValidator<AddShoppingListItemCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new AddShoppingListItemCommand(listId, userId, request.ProductId, request.Quantity);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        AddShoppingListItemOutcome.Added => Results.Ok(result),
        AddShoppingListItemOutcome.ListNotFound => Results.NotFound(),
        AddShoppingListItemOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization()
.WithName("AddShoppingListItem");

app.MapDelete("/api/shopping-lists/{listId:int}/items/{itemId:int}", async (
    int listId,
    int itemId,
    ClaimsPrincipal user,
    ICommandHandler<RemoveShoppingListItemCommand, RemoveShoppingListItemResult> handler,
    IValidator<RemoveShoppingListItemCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new RemoveShoppingListItemCommand(listId, itemId, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        RemoveShoppingListItemOutcome.Removed => Results.Ok(result),
        RemoveShoppingListItemOutcome.ListNotFound => Results.NotFound("Shopping list not found."),
        RemoveShoppingListItemOutcome.ItemNotFound => Results.NotFound("Item not found."),
        RemoveShoppingListItemOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization()
.WithName("RemoveShoppingListItem");

app.MapPost("/api/favorites", async (
    FavoriteRequest request,
    ClaimsPrincipal user,
    ICommandHandler<AddFavoriteCommand, AddFavoriteResult> handler,
    IValidator<AddFavoriteCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new AddFavoriteCommand(userId, request.Type, request.EntityId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(command, cancellationToken));
})
.RequireAuthorization()
.WithName("AddFavorite");

app.MapDelete("/api/favorites", async (
    Domain.Engagement.FavoriteType type,
    int entityId,
    ClaimsPrincipal user,
    ICommandHandler<RemoveFavoriteCommand, RemoveFavoriteResult> handler,
    IValidator<RemoveFavoriteCommand> validator,
    CancellationToken cancellationToken) =>
{
    // DELETE requests can't carry an inferred JSON body in Minimal APIs — type/entityId come from the query string instead.
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new RemoveFavoriteCommand(userId, type, entityId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        RemoveFavoriteOutcome.Removed => Results.Ok(result),
        RemoveFavoriteOutcome.NotFound => Results.NotFound(),
        _ => Results.Problem()
    };
})
.RequireAuthorization()
.WithName("RemoveFavorite");

app.MapGet("/api/favorites", async (
    ClaimsPrincipal user,
    IQueryHandler<GetFavoritesQuery, GetFavoritesResult> handler,
    IValidator<GetFavoritesQuery> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var query = new GetFavoritesQuery(userId);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(query, cancellationToken));
})
.RequireAuthorization()
.WithName("GetFavorites");

app.MapPost("/api/products/{productId:int}/reviews", async (
    int productId,
    SubmitReviewRequest request,
    ClaimsPrincipal user,
    ICommandHandler<SubmitReviewCommand, SubmitReviewResult> handler,
    IValidator<SubmitReviewCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new SubmitReviewCommand(userId, productId, request.StoreId, request.Rating, request.Comment);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(command, cancellationToken));
})
.RequireAuthorization()
.RequireRateLimiting("contributions")
.WithName("SubmitReview");

app.MapGet("/api/products/{productId:int}/reviews", async (
    int productId,
    IQueryHandler<GetReviewsQuery, GetReviewsResult> handler,
    IValidator<GetReviewsQuery> validator,
    CancellationToken cancellationToken) =>
{
    var query = new GetReviewsQuery(productId);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(query, cancellationToken));
})
.WithName("GetReviews");

app.MapPost("/api/reviews/{reviewId:int}/reply", async (
    int reviewId,
    ReplyToReviewRequest request,
    ClaimsPrincipal user,
    ICommandHandler<ReplyToReviewCommand, ReplyToReviewResult> handler,
    IValidator<ReplyToReviewCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new ReplyToReviewCommand(reviewId, userId, request.Message);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        ReplyToReviewOutcome.Replied => Results.Ok(result),
        ReplyToReviewOutcome.ReviewNotFound => Results.NotFound(),
        ReplyToReviewOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("ReplyToReview");

app.MapPost("/api/price-alerts", async (
    CreatePriceAlertRequest request,
    ClaimsPrincipal user,
    ICommandHandler<CreatePriceAlertCommand, CreatePriceAlertResult> handler,
    IValidator<CreatePriceAlertCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new CreatePriceAlertCommand(userId, request.ProductId, request.TargetPrice, request.Currency);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(command, cancellationToken));
})
.RequireAuthorization()
.WithName("CreatePriceAlert");

app.MapGet("/api/price-alerts", async (
    ClaimsPrincipal user,
    IQueryHandler<GetPriceAlertsQuery, GetPriceAlertsResult> handler,
    IValidator<GetPriceAlertsQuery> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var query = new GetPriceAlertsQuery(userId);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(query, cancellationToken));
})
.RequireAuthorization()
.WithName("GetPriceAlerts");

app.MapPost("/api/price-alerts/{alertId:int}/deactivate", async (
    int alertId,
    ClaimsPrincipal user,
    ICommandHandler<DeactivatePriceAlertCommand, DeactivatePriceAlertResult> handler,
    IValidator<DeactivatePriceAlertCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new DeactivatePriceAlertCommand(alertId, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        DeactivatePriceAlertOutcome.Deactivated => Results.Ok(result),
        DeactivatePriceAlertOutcome.NotFound => Results.NotFound(),
        DeactivatePriceAlertOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization()
.WithName("DeactivatePriceAlert");

app.MapPost("/api/device-tokens", async (
    RegisterDeviceTokenRequest request,
    ClaimsPrincipal user,
    ICommandHandler<RegisterDeviceTokenCommand, RegisterDeviceTokenResult> handler,
    IValidator<RegisterDeviceTokenCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new RegisterDeviceTokenCommand(userId, request.Token, request.Platform);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(command, cancellationToken));
})
.RequireAuthorization()
.WithName("RegisterDeviceToken");

app.MapGet("/api/notifications", async (
    ClaimsPrincipal user,
    IQueryHandler<GetNotificationsQuery, GetNotificationsResult> handler,
    IValidator<GetNotificationsQuery> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var query = new GetNotificationsQuery(userId);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(query, cancellationToken));
})
.RequireAuthorization()
.WithName("GetNotifications");

app.MapPost("/api/notifications/{notificationId:int}/read", async (
    int notificationId,
    ClaimsPrincipal user,
    ICommandHandler<MarkNotificationAsReadCommand, MarkNotificationAsReadResult> handler,
    IValidator<MarkNotificationAsReadCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new MarkNotificationAsReadCommand(notificationId, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        MarkNotificationAsReadOutcome.MarkedRead => Results.Ok(result),
        MarkNotificationAsReadOutcome.NotFound => Results.NotFound(),
        MarkNotificationAsReadOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization()
.WithName("MarkNotificationAsRead");

// Loyalty/CRM: customers, per-store loyalty programs/points, gift cards, store credit.
// Customer is a shared registry (keyed by phone number); everything else is StorePartner-scoped
// with ownership checks (loyalty via LoyaltyProgram.StoreId, store credit via StoreId directly).

app.MapPost("/api/customers", async (
    CreateCustomerRequest request,
    ICommandHandler<CreateCustomerCommand, CreateCustomerResult> handler,
    IValidator<CreateCustomerCommand> validator,
    CancellationToken cancellationToken) =>
{
    var command = new CreateCustomerCommand(request.PhoneNumber, request.FullName);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(command, cancellationToken));
})
.RequireAuthorization("StorePartner")
.WithName("CreateCustomer");

app.MapGet("/api/customers/by-phone/{phoneNumber}", async (
    string phoneNumber,
    IQueryHandler<GetCustomerByPhoneQuery, GetCustomerByPhoneResult> handler,
    IValidator<GetCustomerByPhoneQuery> validator,
    CancellationToken cancellationToken) =>
{
    var query = new GetCustomerByPhoneQuery(phoneNumber);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(query, cancellationToken));
})
.RequireAuthorization("StorePartner")
.WithName("GetCustomerByPhone");

app.MapPost("/api/loyalty-programs", async (
    CreateLoyaltyProgramRequest request,
    ClaimsPrincipal user,
    ICommandHandler<CreateLoyaltyProgramCommand, CreateLoyaltyProgramResult> handler,
    IValidator<CreateLoyaltyProgramCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new CreateLoyaltyProgramCommand(request.StoreId, request.PointsPerCurrencyUnit, request.RedemptionRate, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        CreateLoyaltyProgramOutcome.Created => Results.Ok(result),
        CreateLoyaltyProgramOutcome.StoreNotFound => Results.NotFound("Store not found."),
        CreateLoyaltyProgramOutcome.Forbidden => Results.Forbid(),
        CreateLoyaltyProgramOutcome.AlreadyExists => Results.Conflict("This store already has a loyalty program."),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("CreateLoyaltyProgram");

app.MapGet("/api/stores/{storeId:int}/loyalty-program", async (
    int storeId,
    IQueryHandler<GetLoyaltyProgramQuery, GetLoyaltyProgramResult> handler,
    IValidator<GetLoyaltyProgramQuery> validator,
    CancellationToken cancellationToken) =>
{
    var query = new GetLoyaltyProgramQuery(storeId);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(query, cancellationToken));
})
.WithName("GetLoyaltyProgram");

app.MapPost("/api/loyalty-accounts/enroll", async (
    EnrollCustomerInLoyaltyRequest request,
    ClaimsPrincipal user,
    ICommandHandler<EnrollCustomerInLoyaltyCommand, EnrollCustomerInLoyaltyResult> handler,
    IValidator<EnrollCustomerInLoyaltyCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new EnrollCustomerInLoyaltyCommand(request.CustomerId, request.LoyaltyProgramId, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        EnrollCustomerInLoyaltyOutcome.Enrolled => Results.Ok(result),
        EnrollCustomerInLoyaltyOutcome.AlreadyEnrolled => Results.Ok(result),
        EnrollCustomerInLoyaltyOutcome.CustomerNotFound => Results.NotFound("Customer not found."),
        EnrollCustomerInLoyaltyOutcome.ProgramNotFound => Results.NotFound("Loyalty program not found."),
        EnrollCustomerInLoyaltyOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("EnrollCustomerInLoyalty");

app.MapPost("/api/loyalty-accounts/{loyaltyAccountId:int}/earn", async (
    int loyaltyAccountId,
    EarnLoyaltyPointsRequest request,
    ClaimsPrincipal user,
    ICommandHandler<EarnLoyaltyPointsCommand, EarnLoyaltyPointsResult> handler,
    IValidator<EarnLoyaltyPointsCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new EarnLoyaltyPointsCommand(loyaltyAccountId, request.Points, request.SaleTransactionId, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        EarnLoyaltyPointsOutcome.Earned => Results.Ok(result),
        EarnLoyaltyPointsOutcome.AccountNotFound => Results.NotFound(),
        EarnLoyaltyPointsOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("EarnLoyaltyPoints");

app.MapPost("/api/loyalty-accounts/{loyaltyAccountId:int}/redeem", async (
    int loyaltyAccountId,
    RedeemLoyaltyPointsRequest request,
    ClaimsPrincipal user,
    ICommandHandler<RedeemLoyaltyPointsCommand, RedeemLoyaltyPointsResult> handler,
    IValidator<RedeemLoyaltyPointsCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new RedeemLoyaltyPointsCommand(loyaltyAccountId, request.Points, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        RedeemLoyaltyPointsOutcome.Redeemed => Results.Ok(result),
        RedeemLoyaltyPointsOutcome.AccountNotFound => Results.NotFound(),
        RedeemLoyaltyPointsOutcome.Forbidden => Results.Forbid(),
        RedeemLoyaltyPointsOutcome.InsufficientPoints => Results.Conflict("Insufficient points balance."),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("RedeemLoyaltyPoints");

app.MapGet("/api/loyalty-accounts", async (
    int customerId,
    int loyaltyProgramId,
    IQueryHandler<GetLoyaltyAccountQuery, GetLoyaltyAccountResult> handler,
    IValidator<GetLoyaltyAccountQuery> validator,
    CancellationToken cancellationToken) =>
{
    var query = new GetLoyaltyAccountQuery(customerId, loyaltyProgramId);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(query, cancellationToken));
})
.RequireAuthorization("StorePartner")
.WithName("GetLoyaltyAccount");

app.MapPost("/api/gift-cards", async (
    IssueGiftCardRequest request,
    ICommandHandler<IssueGiftCardCommand, IssueGiftCardResult> handler,
    IValidator<IssueGiftCardCommand> validator,
    CancellationToken cancellationToken) =>
{
    var command = new IssueGiftCardCommand(request.Amount, request.Currency, request.ExpiresAt);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(command, cancellationToken));
})
.RequireAuthorization("StorePartner")
.WithName("IssueGiftCard");

app.MapPost("/api/gift-cards/{code}/redeem", async (
    string code,
    RedeemGiftCardRequest request,
    ICommandHandler<RedeemGiftCardCommand, RedeemGiftCardResult> handler,
    IValidator<RedeemGiftCardCommand> validator,
    CancellationToken cancellationToken) =>
{
    var command = new RedeemGiftCardCommand(code, request.Amount);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        RedeemGiftCardOutcome.Redeemed => Results.Ok(result),
        RedeemGiftCardOutcome.NotFound => Results.NotFound(),
        RedeemGiftCardOutcome.Inactive => Results.Conflict("This gift card is inactive."),
        RedeemGiftCardOutcome.Expired => Results.Conflict("This gift card has expired."),
        RedeemGiftCardOutcome.InsufficientBalance => Results.Conflict("Insufficient gift card balance."),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.RequireRateLimiting("sales")
.WithName("RedeemGiftCard");

app.MapGet("/api/gift-cards/{code}", async (
    string code,
    IQueryHandler<GetGiftCardBalanceQuery, GetGiftCardBalanceResult> handler,
    IValidator<GetGiftCardBalanceQuery> validator,
    CancellationToken cancellationToken) =>
{
    var query = new GetGiftCardBalanceQuery(code);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(query, cancellationToken));
})
.WithName("GetGiftCardBalance");

app.MapPost("/api/store-credit/issue", async (
    IssueStoreCreditRequest request,
    ClaimsPrincipal user,
    ICommandHandler<IssueStoreCreditCommand, IssueStoreCreditResult> handler,
    IValidator<IssueStoreCreditCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new IssueStoreCreditCommand(request.StoreId, request.CustomerId, request.Amount, request.Currency, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        IssueStoreCreditOutcome.Issued => Results.Ok(result),
        IssueStoreCreditOutcome.StoreNotFound => Results.NotFound("Store not found."),
        IssueStoreCreditOutcome.CustomerNotFound => Results.NotFound("Customer not found."),
        IssueStoreCreditOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("IssueStoreCredit");

app.MapPost("/api/store-credit/redeem", async (
    RedeemStoreCreditRequest request,
    ClaimsPrincipal user,
    ICommandHandler<RedeemStoreCreditCommand, RedeemStoreCreditResult> handler,
    IValidator<RedeemStoreCreditCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new RedeemStoreCreditCommand(request.StoreId, request.CustomerId, request.Amount, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        RedeemStoreCreditOutcome.Redeemed => Results.Ok(result),
        RedeemStoreCreditOutcome.StoreNotFound => Results.NotFound("Store not found."),
        RedeemStoreCreditOutcome.Forbidden => Results.Forbid(),
        RedeemStoreCreditOutcome.NoCreditOnFile => Results.NotFound("No store credit on file for this customer."),
        RedeemStoreCreditOutcome.InsufficientBalance => Results.Conflict("Insufficient store credit balance."),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("RedeemStoreCredit");

app.MapGet("/api/store-credit", async (
    int storeId,
    int customerId,
    ClaimsPrincipal user,
    IQueryHandler<GetStoreCreditBalanceQuery, GetStoreCreditBalanceResult> handler,
    IValidator<GetStoreCreditBalanceQuery> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var query = new GetStoreCreditBalanceQuery(storeId, customerId, userId);

    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(query, cancellationToken);
    return result.Outcome switch
    {
        GetStoreCreditBalanceOutcome.Found => Results.Ok(result),
        GetStoreCreditBalanceOutcome.StoreNotFound => Results.NotFound(),
        GetStoreCreditBalanceOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("GetStoreCreditBalance");

// Supply chain: purchase orders (with receipt actually crediting stock), inter-store transfers
// (both ends must belong to the same owner), low-stock reorder alerts, product bundles.

app.MapPost("/api/purchase-orders", async (
    CreatePurchaseOrderRequest request,
    ClaimsPrincipal user,
    ICommandHandler<CreatePurchaseOrderCommand, CreatePurchaseOrderResult> handler,
    IValidator<CreatePurchaseOrderCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new CreatePurchaseOrderCommand(request.StoreId, request.SupplierId, request.Lines, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        CreatePurchaseOrderOutcome.Created => Results.Ok(result),
        CreatePurchaseOrderOutcome.StoreNotFound => Results.NotFound("Store not found."),
        CreatePurchaseOrderOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("CreatePurchaseOrder");

app.MapPost("/api/purchase-orders/{purchaseOrderId:int}/submit", async (
    int purchaseOrderId,
    ClaimsPrincipal user,
    ICommandHandler<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult> handler,
    IValidator<SubmitPurchaseOrderCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new SubmitPurchaseOrderCommand(purchaseOrderId, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        SubmitPurchaseOrderOutcome.Submitted => Results.Ok(result),
        SubmitPurchaseOrderOutcome.NotFound => Results.NotFound(),
        SubmitPurchaseOrderOutcome.Forbidden => Results.Forbid(),
        SubmitPurchaseOrderOutcome.NotDraft => Results.Conflict("This purchase order is no longer a draft."),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("SubmitPurchaseOrder");

app.MapPost("/api/purchase-orders/{purchaseOrderId:int}/receive", async (
    int purchaseOrderId,
    ClaimsPrincipal user,
    ICommandHandler<ReceivePurchaseOrderCommand, ReceivePurchaseOrderResult> handler,
    IValidator<ReceivePurchaseOrderCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new ReceivePurchaseOrderCommand(purchaseOrderId, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        ReceivePurchaseOrderOutcome.Received => Results.Ok(result),
        ReceivePurchaseOrderOutcome.NotFound => Results.NotFound(),
        ReceivePurchaseOrderOutcome.Forbidden => Results.Forbid(),
        ReceivePurchaseOrderOutcome.NotSubmitted => Results.Conflict("This purchase order has not been submitted."),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("ReceivePurchaseOrder");

app.MapGet("/api/stores/{storeId:int}/purchase-orders", async (
    int storeId,
    ClaimsPrincipal user,
    IQueryHandler<GetPurchaseOrdersQuery, GetPurchaseOrdersResult> handler,
    IValidator<GetPurchaseOrdersQuery> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var query = new GetPurchaseOrdersQuery(storeId, userId);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(query, cancellationToken);
    return result.Outcome switch
    {
        GetPurchaseOrdersOutcome.Found => Results.Ok(result),
        GetPurchaseOrdersOutcome.StoreNotFound => Results.NotFound(),
        GetPurchaseOrdersOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("GetPurchaseOrders");

app.MapPost("/api/stock-transfers", async (
    InitiateStockTransferRequest request,
    ClaimsPrincipal user,
    ICommandHandler<InitiateStockTransferCommand, InitiateStockTransferResult> handler,
    IValidator<InitiateStockTransferCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new InitiateStockTransferCommand(request.ProductId, request.FromStoreId, request.ToStoreId, request.Quantity, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        InitiateStockTransferOutcome.Initiated => Results.Ok(result),
        InitiateStockTransferOutcome.FromStoreNotFound => Results.NotFound("Source store not found."),
        InitiateStockTransferOutcome.ToStoreNotFound => Results.NotFound("Destination store not found."),
        InitiateStockTransferOutcome.Forbidden => Results.Forbid(),
        InitiateStockTransferOutcome.InsufficientStock => Results.Conflict("Insufficient stock at the source store."),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("InitiateStockTransfer");

app.MapPost("/api/stock-transfers/{stockTransferId:int}/complete", async (
    int stockTransferId,
    ClaimsPrincipal user,
    ICommandHandler<CompleteStockTransferCommand, CompleteStockTransferResult> handler,
    IValidator<CompleteStockTransferCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new CompleteStockTransferCommand(stockTransferId, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        CompleteStockTransferOutcome.Completed => Results.Ok(result),
        CompleteStockTransferOutcome.NotFound => Results.NotFound(),
        CompleteStockTransferOutcome.Forbidden => Results.Forbid(),
        CompleteStockTransferOutcome.NotInTransit => Results.Conflict("This transfer is not in transit."),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("CompleteStockTransfer");

app.MapGet("/api/stores/{storeId:int}/stock-transfers", async (
    int storeId,
    ClaimsPrincipal user,
    IQueryHandler<GetStockTransfersQuery, GetStockTransfersResult> handler,
    IValidator<GetStockTransfersQuery> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var query = new GetStockTransfersQuery(storeId, userId);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(query, cancellationToken);
    return result.Outcome switch
    {
        GetStockTransfersOutcome.Found => Results.Ok(result),
        GetStockTransfersOutcome.StoreNotFound => Results.NotFound(),
        GetStockTransfersOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("GetStockTransfers");

app.MapPost("/api/reorder-rules", async (
    CreateReorderRuleRequest request,
    ClaimsPrincipal user,
    ICommandHandler<CreateReorderRuleCommand, CreateReorderRuleResult> handler,
    IValidator<CreateReorderRuleCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new CreateReorderRuleCommand(request.StoreId, request.ProductId, request.ThresholdQuantity, request.ReorderQuantity, request.PreferredSupplierId, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        CreateReorderRuleOutcome.Created => Results.Ok(result),
        CreateReorderRuleOutcome.StoreNotFound => Results.NotFound("Store not found."),
        CreateReorderRuleOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("CreateReorderRule");

app.MapGet("/api/stores/{storeId:int}/reorder-alerts", async (
    int storeId,
    ClaimsPrincipal user,
    IQueryHandler<GetReorderAlertsQuery, GetReorderAlertsResult> handler,
    IValidator<GetReorderAlertsQuery> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var query = new GetReorderAlertsQuery(storeId, userId);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(query, cancellationToken);
    return result.Outcome switch
    {
        GetReorderAlertsOutcome.Found => Results.Ok(result),
        GetReorderAlertsOutcome.StoreNotFound => Results.NotFound(),
        GetReorderAlertsOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("GetReorderAlerts");

app.MapPost("/api/product-bundles", async (
    CreateProductBundleRequest request,
    ClaimsPrincipal user,
    ICommandHandler<CreateProductBundleCommand, CreateProductBundleResult> handler,
    IValidator<CreateProductBundleCommand> validator,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var command = new CreateProductBundleCommand(request.StoreId, request.Name, request.BundlePrice, request.Currency, request.Items, userId);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        CreateProductBundleOutcome.Created => Results.Ok(result),
        CreateProductBundleOutcome.StoreNotFound => Results.NotFound("Store not found."),
        CreateProductBundleOutcome.Forbidden => Results.Forbid(),
        _ => Results.Problem()
    };
})
.RequireAuthorization("StorePartner")
.WithName("CreateProductBundle");

app.MapGet("/api/stores/{storeId:int}/product-bundles", async (
    int storeId,
    IQueryHandler<GetProductBundlesQuery, GetProductBundlesResult> handler,
    IValidator<GetProductBundlesQuery> validator,
    CancellationToken cancellationToken) =>
{
    var query = new GetProductBundlesQuery(storeId);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    return Results.Ok(await handler.Handle(query, cancellationToken));
})
.WithName("GetProductBundles");

// Catalog reference data (Brand/Category/TaxRate/Supplier) — shared, platform-wide lookup tables
// that Product/StockMovement already reference by FK. Brand/Category/TaxRate are curated centrally
// (Admin) to keep the taxonomy consistent; Supplier is StorePartner-managed (any partner arranges
// their own suppliers). All four lists are public — the frontend needs them for dropdowns/browsing.

app.MapPost("/api/catalog/brands", async (
    CreateBrandCommand command,
    ICommandHandler<CreateBrandCommand, CreateBrandResult> handler,
    IValidator<CreateBrandCommand> validator,
    CancellationToken cancellationToken) =>
{
    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return Results.Ok(result);
})
.RequireAuthorization("Admin")
.WithName("CreateBrand");

app.MapGet("/api/catalog/brands", async (
    IQueryHandler<GetBrandsQuery, GetBrandsResult> handler,
    CancellationToken cancellationToken) =>
    Results.Ok(await handler.Handle(new GetBrandsQuery(), cancellationToken)))
.WithName("GetBrands");

app.MapPost("/api/catalog/categories", async (
    CreateCategoryCommand command,
    ICommandHandler<CreateCategoryCommand, CreateCategoryResult> handler,
    IValidator<CreateCategoryCommand> validator,
    CancellationToken cancellationToken) =>
{
    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return result.Outcome switch
    {
        CreateCategoryOutcome.Created => Results.Ok(result),
        CreateCategoryOutcome.ParentCategoryNotFound => Results.NotFound("Parent category not found."),
        _ => Results.Problem()
    };
})
.RequireAuthorization("Admin")
.WithName("CreateCategory");

app.MapGet("/api/catalog/categories", async (
    IQueryHandler<GetCategoriesQuery, GetCategoriesResult> handler,
    CancellationToken cancellationToken) =>
    Results.Ok(await handler.Handle(new GetCategoriesQuery(), cancellationToken)))
.WithName("GetCategories");

app.MapPost("/api/catalog/tax-rates", async (
    CreateTaxRateCommand command,
    ICommandHandler<CreateTaxRateCommand, CreateTaxRateResult> handler,
    IValidator<CreateTaxRateCommand> validator,
    CancellationToken cancellationToken) =>
{
    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return Results.Ok(result);
})
.RequireAuthorization("Admin")
.WithName("CreateTaxRate");

app.MapGet("/api/catalog/tax-rates", async (
    IQueryHandler<GetTaxRatesQuery, GetTaxRatesResult> handler,
    CancellationToken cancellationToken) =>
    Results.Ok(await handler.Handle(new GetTaxRatesQuery(), cancellationToken)))
.WithName("GetTaxRates");

app.MapPost("/api/suppliers", async (
    CreateSupplierCommand command,
    ICommandHandler<CreateSupplierCommand, CreateSupplierResult> handler,
    IValidator<CreateSupplierCommand> validator,
    CancellationToken cancellationToken) =>
{
    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var result = await handler.Handle(command, cancellationToken);
    return Results.Ok(result);
})
.RequireAuthorization("StorePartner")
.WithName("CreateSupplier");

app.MapGet("/api/suppliers", async (
    IQueryHandler<GetSuppliersQuery, GetSuppliersResult> handler,
    CancellationToken cancellationToken) =>
    Results.Ok(await handler.Handle(new GetSuppliersQuery(), cancellationToken)))
.RequireAuthorization("StorePartner")
.WithName("GetSuppliers");

app.Run();

// Identifies the actual image format from its magic bytes, ignoring the client-supplied
// Content-Type header and filename extension (both are attacker-controlled).
static async Task<string?> DetectImageExtensionAsync(IFormFile file, CancellationToken cancellationToken)
{
    var header = new byte[8];
    await using var stream = file.OpenReadStream();
    var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

    if (bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        return ".jpg";

    if (bytesRead >= 8
        && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
        && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        return ".png";

    return null;
}

internal sealed record OpenCashierShiftRequest(int StoreId, decimal OpeningCash, string Currency);
internal sealed record CloseCashierShiftRequest(decimal ClosingCash);
internal sealed record ProcessReturnRequest(IReadOnlyList<Application.Sales.Commands.ProcessReturn.ProcessReturnLineInput> Lines, string Reason);
internal sealed record CreatePurchaseOrderRequest(int StoreId, int SupplierId, IReadOnlyList<Application.Inventory.Commands.CreatePurchaseOrder.CreatePurchaseOrderLineInput> Lines);
internal sealed record InitiateStockTransferRequest(int ProductId, int FromStoreId, int ToStoreId, int Quantity);
internal sealed record CreateReorderRuleRequest(int StoreId, int ProductId, int ThresholdQuantity, int ReorderQuantity, int? PreferredSupplierId);
internal sealed record CreateProductBundleRequest(int StoreId, string Name, decimal BundlePrice, string Currency, IReadOnlyList<Application.Catalog.Commands.CreateProductBundle.CreateProductBundleItemInput> Items);
internal sealed record CreateCustomerRequest(string PhoneNumber, string? FullName);
internal sealed record CreateLoyaltyProgramRequest(int StoreId, decimal PointsPerCurrencyUnit, decimal RedemptionRate);
internal sealed record EnrollCustomerInLoyaltyRequest(int CustomerId, int LoyaltyProgramId);
internal sealed record EarnLoyaltyPointsRequest(int Points, int? SaleTransactionId);
internal sealed record RedeemLoyaltyPointsRequest(int Points);
internal sealed record IssueGiftCardRequest(decimal Amount, string Currency, DateTimeOffset? ExpiresAt);
internal sealed record RedeemGiftCardRequest(decimal Amount);
internal sealed record IssueStoreCreditRequest(int StoreId, int CustomerId, decimal Amount, string Currency);
internal sealed record RedeemStoreCreditRequest(int StoreId, int CustomerId, decimal Amount);
internal sealed record CreateShoppingListRequest(string Name);
internal sealed record AddShoppingListItemRequest(int ProductId, int Quantity);
internal sealed record FavoriteRequest(Domain.Engagement.FavoriteType Type, int EntityId);
internal sealed record SubmitReviewRequest(int? StoreId, int Rating, string Comment);
internal sealed record ReplyToReviewRequest(string Message);
internal sealed record CreatePriceAlertRequest(int ProductId, decimal TargetPrice, string Currency);
internal sealed record RegisterDeviceTokenRequest(string Token, Domain.Notifications.DevicePlatform Platform);
internal sealed record CreateStoreRequest(string Name, string Address, double Latitude, double Longitude);
internal sealed record SetCostPriceRequest(int StoreId, int ProductId, decimal Amount, string Currency);
internal sealed record SubmitPriceUpdateRequest(int ProductId, int StoreId, decimal Price, string Currency);
internal sealed record ProcessSaleRequest(int StoreId, string IdempotencyKey, string Currency, IReadOnlyList<ProcessSaleLine> Lines);
internal sealed record VoidSaleRequest(string Reason);
internal sealed record RecordStockReceiptRequest(int StoreId, int ProductId, int Quantity, int? SupplierId);
internal sealed record ReportOutOfStockRequest(int ProductId, int? StoreId, string Description);
internal sealed record PublishExpiringOfferRequest(int StoreId, int ProductId, decimal OriginalPrice, decimal DiscountedPrice, string Currency, DateTimeOffset ExpiresAt);
internal sealed record ModerateNewProductRequest(bool Approve, string? Reason);
internal sealed record ModerateReportRequest(bool Resolve, string? Reason);
