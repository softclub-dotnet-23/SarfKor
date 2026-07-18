using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Application;
using Application.Common;
using Application.Feedback.Commands.ModerateReport;
using Application.Feedback.Commands.ReportOutOfStock;
using Application.Identity.Commands.Login;
using Application.Identity.Commands.RefreshToken;
using Application.Identity.Commands.Register;
using Application.Inventory.Commands.RecordStockReceipt;
using Application.Inventory.Queries.GetStockLevel;
using Application.Offers.Commands.PublishExpiringOffer;
using Application.Pricing.Commands.SubmitPriceUpdate;
using Application.Products.Commands.ModerateNewProduct;
using Application.Products.Queries.CompareStoresForShoppingList;
using Application.Products.Queries.GetTopSellingProducts;
using Application.Products.Queries.ScanBarcode;
using Application.Receipts.Commands.VerifyReceipt;
using Application.Sales.Commands.ProcessSale;
using Application.Sales.Commands.VoidSale;
using Application.Sales.Queries.GetDailySalesReport;
using Application.Sales.Queries.GetProfitReport;
using Application.Stores.Queries.GetStoreDashboard;
using FluentValidation;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

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

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

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

app.Run();

internal sealed record SubmitPriceUpdateRequest(int ProductId, int StoreId, decimal Price, string Currency);
internal sealed record ProcessSaleRequest(int StoreId, string IdempotencyKey, string Currency, IReadOnlyList<ProcessSaleLine> Lines);
internal sealed record VoidSaleRequest(string Reason);
internal sealed record RecordStockReceiptRequest(int StoreId, int ProductId, int Quantity, int? SupplierId);
internal sealed record ReportOutOfStockRequest(int ProductId, int? StoreId, string Description);
internal sealed record PublishExpiringOfferRequest(int StoreId, int ProductId, decimal OriginalPrice, decimal DiscountedPrice, string Currency, DateTimeOffset ExpiresAt);
internal sealed record ModerateNewProductRequest(bool Approve, string? Reason);
internal sealed record ModerateReportRequest(bool Resolve, string? Reason);
