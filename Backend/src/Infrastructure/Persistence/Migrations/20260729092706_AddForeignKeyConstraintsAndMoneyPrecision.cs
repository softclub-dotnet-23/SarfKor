using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyConstraintsAndMoneyPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Balance_Amount",
                table: "StoreCredits",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPriceAtSale_Amount",
                table: "SaleLineItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "RefundAmount_Amount",
                table: "ReturnLineItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price_Amount",
                table: "ReceiptLineItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitCost_Amount",
                table: "PurchaseOrderLineItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "BundlePrice_Amount",
                table: "ProductBundles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price_Amount",
                table: "PriceEntries",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TargetPrice_Amount",
                table: "PriceAlerts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount_Amount",
                table: "Payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance_Amount",
                table: "GiftCards",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "OriginalPrice_Amount",
                table: "ExpiringOffers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountedPrice_Amount",
                table: "ExpiringOffers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount_Amount",
                table: "CostPrices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount_Amount",
                table: "Commissions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "OpeningCash_Amount",
                table: "CashierShifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExpectedCash_Amount",
                table: "CashierShifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ClosingCash_Amount",
                table: "CashierShifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_UserId",
                table: "UserConsents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_CategoryId",
                table: "TaxRates",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_OwnerUserId",
                table: "Stores",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreEmployees_StoreId",
                table: "StoreEmployees",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreEmployees_UserId",
                table: "StoreEmployees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreCredits_CustomerId",
                table: "StoreCredits",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreCredits_StoreId",
                table: "StoreCredits",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_FromStoreId",
                table: "StockTransfers",
                column: "FromStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_InitiatedByUserId",
                table: "StockTransfers",
                column: "InitiatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ProductId",
                table: "StockTransfers",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ToStoreId",
                table: "StockTransfers",
                column: "ToStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_PerformedByUserId",
                table: "StockMovements",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_RelatedSaleTransactionId",
                table: "StockMovements",
                column: "RelatedSaleTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StoreId",
                table: "StockMovements",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SupplierId",
                table: "StockMovements",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLevels_StoreId",
                table: "StockLevels",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingLists_UserId",
                table: "ShoppingLists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItems_ProductId",
                table: "ShoppingListItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_UserId",
                table: "SecurityEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Scans_ProductId",
                table: "Scans",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Scans_StoreId",
                table: "Scans",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Scans_UserId",
                table: "Scans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleTransactions_CashierUserId",
                table: "SaleTransactions",
                column: "CashierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleTransactions_CustomerId",
                table: "SaleTransactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleTransactions_VoidedByUserId",
                table: "SaleTransactions",
                column: "VoidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_ProcessedByUserId",
                table: "SaleReturns",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_SaleTransactionId",
                table: "SaleReturns",
                column: "SaleTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleLineItems_ProductId",
                table: "SaleLineItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductId",
                table: "Reviews",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_StoreId",
                table: "Reviews",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId",
                table: "Reviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewReplies_ReviewId",
                table: "ReviewReplies",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewReplies_StorePartnerUserId",
                table: "ReviewReplies",
                column: "StorePartnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLineItems_SaleLineItemId",
                table: "ReturnLineItems",
                column: "SaleLineItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ProductId",
                table: "Reports",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ResolvedByAdminUserId",
                table: "Reports",
                column: "ResolvedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_StoreId",
                table: "Reports",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_UserId",
                table: "Reports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDisputes_DisputedByUserId",
                table: "ReportDisputes",
                column: "DisputedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDisputes_ReportId",
                table: "ReportDisputes",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReorderRules_PreferredSupplierId",
                table: "ReorderRules",
                column: "PreferredSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_ReorderRules_ProductId",
                table: "ReorderRules",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ReorderRules_StoreId",
                table: "ReorderRules",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_StoreId",
                table: "Receipts",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_UserId",
                table: "Receipts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptLineItems_ProductId",
                table: "ReceiptLineItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedByUserId",
                table: "PurchaseOrders",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_StoreId",
                table: "PurchaseOrders",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLineItems_ProductId",
                table: "PurchaseOrderLineItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_CategoryId",
                table: "Promotions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_ProductId",
                table: "Promotions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_StoreId",
                table: "Promotions",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSubmissions_BrandId",
                table: "ProductSubmissions",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSubmissions_CategoryId",
                table: "ProductSubmissions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSubmissions_ModeratedByAdminUserId",
                table: "ProductSubmissions",
                column: "ModeratedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSubmissions_SubmittedByUserId",
                table: "ProductSubmissions",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BrandId",
                table: "Products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TaxRateId",
                table: "Products",
                column: "TaxRateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBundles_StoreId",
                table: "ProductBundles",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBundleItems_ProductId",
                table: "ProductBundleItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceEntryDisputes_DisputedByUserId",
                table: "PriceEntryDisputes",
                column: "DisputedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceEntryDisputes_PriceEntryId",
                table: "PriceEntryDisputes",
                column: "PriceEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceEntries_ProductId",
                table: "PriceEntries",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceEntries_StoreId",
                table: "PriceEntries",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceEntries_SubmittedByUserId",
                table: "PriceEntries",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_ProductId",
                table: "PriceAlerts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_UserId",
                table: "PriceAlerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SaleTransactionId",
                table: "Payments",
                column: "SaleTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_LoyaltyAccountId",
                table: "LoyaltyTransactions",
                column: "LoyaltyAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_SaleTransactionId",
                table: "LoyaltyTransactions",
                column: "SaleTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPrograms_StoreId",
                table: "LoyaltyPrograms",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyAccounts_CustomerId",
                table: "LoyaltyAccounts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyAccounts_LoyaltyProgramId",
                table: "LoyaltyAccounts",
                column: "LoyaltyProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalReceipts_SaleTransactionId",
                table: "FiscalReceipts",
                column: "SaleTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId",
                table: "Favorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpiringOffers_ProductId",
                table: "ExpiringOffers",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpiringOffers_StoreId",
                table: "ExpiringOffers",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_UserId",
                table: "DeviceTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CostPrices_ProductId",
                table: "CostPrices",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CostPrices_SetByUserId",
                table: "CostPrices",
                column: "SetByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CostPrices_StoreId",
                table: "CostPrices",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributorTrustScores_UserId",
                table: "ContributorTrustScores",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_CashierUserId",
                table: "Commissions",
                column: "CashierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_SaleTransactionId",
                table: "Commissions",
                column: "SaleTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CashierShifts_CashierUserId",
                table: "CashierShifts",
                column: "CashierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CashierShifts_StoreId",
                table: "CashierShifts",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PerformedByUserId",
                table: "AuditLogs",
                column: "PerformedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AspNetUsers_PerformedByUserId",
                table: "AuditLogs",
                column: "PerformedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashierShifts_AspNetUsers_CashierUserId",
                table: "CashierShifts",
                column: "CashierUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashierShifts_Stores_StoreId",
                table: "CashierShifts",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Commissions_AspNetUsers_CashierUserId",
                table: "Commissions",
                column: "CashierUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Commissions_SaleTransactions_SaleTransactionId",
                table: "Commissions",
                column: "SaleTransactionId",
                principalTable: "SaleTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContributorTrustScores_AspNetUsers_UserId",
                table: "ContributorTrustScores",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CostPrices_AspNetUsers_SetByUserId",
                table: "CostPrices",
                column: "SetByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CostPrices_Products_ProductId",
                table: "CostPrices",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CostPrices_Stores_StoreId",
                table: "CostPrices",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceTokens_AspNetUsers_UserId",
                table: "DeviceTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpiringOffers_Products_ProductId",
                table: "ExpiringOffers",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpiringOffers_Stores_StoreId",
                table: "ExpiringOffers",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_AspNetUsers_UserId",
                table: "Favorites",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalReceipts_SaleTransactions_SaleTransactionId",
                table: "FiscalReceipts",
                column: "SaleTransactionId",
                principalTable: "SaleTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LoyaltyAccounts_Customers_CustomerId",
                table: "LoyaltyAccounts",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LoyaltyAccounts_LoyaltyPrograms_LoyaltyProgramId",
                table: "LoyaltyAccounts",
                column: "LoyaltyProgramId",
                principalTable: "LoyaltyPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LoyaltyPrograms_Stores_StoreId",
                table: "LoyaltyPrograms",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LoyaltyTransactions_LoyaltyAccounts_LoyaltyAccountId",
                table: "LoyaltyTransactions",
                column: "LoyaltyAccountId",
                principalTable: "LoyaltyAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LoyaltyTransactions_SaleTransactions_SaleTransactionId",
                table: "LoyaltyTransactions",
                column: "SaleTransactionId",
                principalTable: "SaleTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_SaleTransactions_SaleTransactionId",
                table: "Payments",
                column: "SaleTransactionId",
                principalTable: "SaleTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceAlerts_AspNetUsers_UserId",
                table: "PriceAlerts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceAlerts_Products_ProductId",
                table: "PriceAlerts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceEntries_AspNetUsers_SubmittedByUserId",
                table: "PriceEntries",
                column: "SubmittedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceEntries_Products_ProductId",
                table: "PriceEntries",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceEntries_Stores_StoreId",
                table: "PriceEntries",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceEntryDisputes_AspNetUsers_DisputedByUserId",
                table: "PriceEntryDisputes",
                column: "DisputedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceEntryDisputes_PriceEntries_PriceEntryId",
                table: "PriceEntryDisputes",
                column: "PriceEntryId",
                principalTable: "PriceEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductBundleItems_Products_ProductId",
                table: "ProductBundleItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductBundles_Stores_StoreId",
                table: "ProductBundles",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductImages_Products_ProductId",
                table: "ProductImages",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Brands_BrandId",
                table: "Products",
                column: "BrandId",
                principalTable: "Brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_TaxRates_TaxRateId",
                table: "Products",
                column: "TaxRateId",
                principalTable: "TaxRates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSubmissions_AspNetUsers_ModeratedByAdminUserId",
                table: "ProductSubmissions",
                column: "ModeratedByAdminUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSubmissions_AspNetUsers_SubmittedByUserId",
                table: "ProductSubmissions",
                column: "SubmittedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSubmissions_Brands_BrandId",
                table: "ProductSubmissions",
                column: "BrandId",
                principalTable: "Brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSubmissions_Categories_CategoryId",
                table: "ProductSubmissions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_Categories_CategoryId",
                table: "Promotions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_Products_ProductId",
                table: "Promotions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_Stores_StoreId",
                table: "Promotions",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLineItems_Products_ProductId",
                table: "PurchaseOrderLineItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_CreatedByUserId",
                table: "PurchaseOrders",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Stores_StoreId",
                table: "PurchaseOrders",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiptLineItems_Products_ProductId",
                table: "ReceiptLineItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_AspNetUsers_UserId",
                table: "Receipts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_Stores_StoreId",
                table: "Receipts",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_AspNetUsers_UserId",
                table: "RefreshTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReorderRules_Products_ProductId",
                table: "ReorderRules",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReorderRules_Stores_StoreId",
                table: "ReorderRules",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReorderRules_Suppliers_PreferredSupplierId",
                table: "ReorderRules",
                column: "PreferredSupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportDisputes_AspNetUsers_DisputedByUserId",
                table: "ReportDisputes",
                column: "DisputedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportDisputes_Reports_ReportId",
                table: "ReportDisputes",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AspNetUsers_ResolvedByAdminUserId",
                table: "Reports",
                column: "ResolvedByAdminUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AspNetUsers_UserId",
                table: "Reports",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Products_ProductId",
                table: "Reports",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Stores_StoreId",
                table: "Reports",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnLineItems_SaleLineItems_SaleLineItemId",
                table: "ReturnLineItems",
                column: "SaleLineItemId",
                principalTable: "SaleLineItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewReplies_AspNetUsers_StorePartnerUserId",
                table: "ReviewReplies",
                column: "StorePartnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewReplies_Reviews_ReviewId",
                table: "ReviewReplies",
                column: "ReviewId",
                principalTable: "Reviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AspNetUsers_UserId",
                table: "Reviews",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Products_ProductId",
                table: "Reviews",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Stores_StoreId",
                table: "Reviews",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleLineItems_Products_ProductId",
                table: "SaleLineItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleReturns_AspNetUsers_ProcessedByUserId",
                table: "SaleReturns",
                column: "ProcessedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleReturns_SaleTransactions_SaleTransactionId",
                table: "SaleReturns",
                column: "SaleTransactionId",
                principalTable: "SaleTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleTransactions_AspNetUsers_CashierUserId",
                table: "SaleTransactions",
                column: "CashierUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleTransactions_AspNetUsers_VoidedByUserId",
                table: "SaleTransactions",
                column: "VoidedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleTransactions_Customers_CustomerId",
                table: "SaleTransactions",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleTransactions_Stores_StoreId",
                table: "SaleTransactions",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scans_AspNetUsers_UserId",
                table: "Scans",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Scans_Products_ProductId",
                table: "Scans",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scans_Stores_StoreId",
                table: "Scans",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SecurityEvents_AspNetUsers_UserId",
                table: "SecurityEvents",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingListItems_Products_ProductId",
                table: "ShoppingListItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingLists_AspNetUsers_UserId",
                table: "ShoppingLists",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLevels_Products_ProductId",
                table: "StockLevels",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLevels_Stores_StoreId",
                table: "StockLevels",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_AspNetUsers_PerformedByUserId",
                table: "StockMovements",
                column: "PerformedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Products_ProductId",
                table: "StockMovements",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_SaleTransactions_RelatedSaleTransactionId",
                table: "StockMovements",
                column: "RelatedSaleTransactionId",
                principalTable: "SaleTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Stores_StoreId",
                table: "StockMovements",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Suppliers_SupplierId",
                table: "StockMovements",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_AspNetUsers_InitiatedByUserId",
                table: "StockTransfers",
                column: "InitiatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_Products_ProductId",
                table: "StockTransfers",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_Stores_FromStoreId",
                table: "StockTransfers",
                column: "FromStoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_Stores_ToStoreId",
                table: "StockTransfers",
                column: "ToStoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StoreCredits_Customers_CustomerId",
                table: "StoreCredits",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StoreCredits_Stores_StoreId",
                table: "StoreCredits",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StoreEmployees_AspNetUsers_UserId",
                table: "StoreEmployees",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StoreEmployees_Stores_StoreId",
                table: "StoreEmployees",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Stores_AspNetUsers_OwnerUserId",
                table: "Stores",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxRates_Categories_CategoryId",
                table: "TaxRates",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserConsents_AspNetUsers_UserId",
                table: "UserConsents",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_AspNetUsers_UserId",
                table: "UserProfiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AspNetUsers_PerformedByUserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_CashierShifts_AspNetUsers_CashierUserId",
                table: "CashierShifts");

            migrationBuilder.DropForeignKey(
                name: "FK_CashierShifts_Stores_StoreId",
                table: "CashierShifts");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Commissions_AspNetUsers_CashierUserId",
                table: "Commissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Commissions_SaleTransactions_SaleTransactionId",
                table: "Commissions");

            migrationBuilder.DropForeignKey(
                name: "FK_ContributorTrustScores_AspNetUsers_UserId",
                table: "ContributorTrustScores");

            migrationBuilder.DropForeignKey(
                name: "FK_CostPrices_AspNetUsers_SetByUserId",
                table: "CostPrices");

            migrationBuilder.DropForeignKey(
                name: "FK_CostPrices_Products_ProductId",
                table: "CostPrices");

            migrationBuilder.DropForeignKey(
                name: "FK_CostPrices_Stores_StoreId",
                table: "CostPrices");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceTokens_AspNetUsers_UserId",
                table: "DeviceTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpiringOffers_Products_ProductId",
                table: "ExpiringOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpiringOffers_Stores_StoreId",
                table: "ExpiringOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_Favorites_AspNetUsers_UserId",
                table: "Favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_FiscalReceipts_SaleTransactions_SaleTransactionId",
                table: "FiscalReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_LoyaltyAccounts_Customers_CustomerId",
                table: "LoyaltyAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_LoyaltyAccounts_LoyaltyPrograms_LoyaltyProgramId",
                table: "LoyaltyAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_LoyaltyPrograms_Stores_StoreId",
                table: "LoyaltyPrograms");

            migrationBuilder.DropForeignKey(
                name: "FK_LoyaltyTransactions_LoyaltyAccounts_LoyaltyAccountId",
                table: "LoyaltyTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_LoyaltyTransactions_SaleTransactions_SaleTransactionId",
                table: "LoyaltyTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_SaleTransactions_SaleTransactionId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceAlerts_AspNetUsers_UserId",
                table: "PriceAlerts");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceAlerts_Products_ProductId",
                table: "PriceAlerts");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceEntries_AspNetUsers_SubmittedByUserId",
                table: "PriceEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceEntries_Products_ProductId",
                table: "PriceEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceEntries_Stores_StoreId",
                table: "PriceEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceEntryDisputes_AspNetUsers_DisputedByUserId",
                table: "PriceEntryDisputes");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceEntryDisputes_PriceEntries_PriceEntryId",
                table: "PriceEntryDisputes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductBundleItems_Products_ProductId",
                table: "ProductBundleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductBundles_Stores_StoreId",
                table: "ProductBundles");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductImages_Products_ProductId",
                table: "ProductImages");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Brands_BrandId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_TaxRates_TaxRateId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSubmissions_AspNetUsers_ModeratedByAdminUserId",
                table: "ProductSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSubmissions_AspNetUsers_SubmittedByUserId",
                table: "ProductSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSubmissions_Brands_BrandId",
                table: "ProductSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSubmissions_Categories_CategoryId",
                table: "ProductSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_Categories_CategoryId",
                table: "Promotions");

            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_Products_ProductId",
                table: "Promotions");

            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_Stores_StoreId",
                table: "Promotions");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLineItems_Products_ProductId",
                table: "PurchaseOrderLineItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_CreatedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Stores_StoreId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptLineItems_Products_ProductId",
                table: "ReceiptLineItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_AspNetUsers_UserId",
                table: "Receipts");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_Stores_StoreId",
                table: "Receipts");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_AspNetUsers_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_ReorderRules_Products_ProductId",
                table: "ReorderRules");

            migrationBuilder.DropForeignKey(
                name: "FK_ReorderRules_Stores_StoreId",
                table: "ReorderRules");

            migrationBuilder.DropForeignKey(
                name: "FK_ReorderRules_Suppliers_PreferredSupplierId",
                table: "ReorderRules");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportDisputes_AspNetUsers_DisputedByUserId",
                table: "ReportDisputes");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportDisputes_Reports_ReportId",
                table: "ReportDisputes");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AspNetUsers_ResolvedByAdminUserId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AspNetUsers_UserId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Products_ProductId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Stores_StoreId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnLineItems_SaleLineItems_SaleLineItemId",
                table: "ReturnLineItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ReviewReplies_AspNetUsers_StorePartnerUserId",
                table: "ReviewReplies");

            migrationBuilder.DropForeignKey(
                name: "FK_ReviewReplies_Reviews_ReviewId",
                table: "ReviewReplies");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AspNetUsers_UserId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Products_ProductId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Stores_StoreId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleLineItems_Products_ProductId",
                table: "SaleLineItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleReturns_AspNetUsers_ProcessedByUserId",
                table: "SaleReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleReturns_SaleTransactions_SaleTransactionId",
                table: "SaleReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleTransactions_AspNetUsers_CashierUserId",
                table: "SaleTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleTransactions_AspNetUsers_VoidedByUserId",
                table: "SaleTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleTransactions_Customers_CustomerId",
                table: "SaleTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleTransactions_Stores_StoreId",
                table: "SaleTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Scans_AspNetUsers_UserId",
                table: "Scans");

            migrationBuilder.DropForeignKey(
                name: "FK_Scans_Products_ProductId",
                table: "Scans");

            migrationBuilder.DropForeignKey(
                name: "FK_Scans_Stores_StoreId",
                table: "Scans");

            migrationBuilder.DropForeignKey(
                name: "FK_SecurityEvents_AspNetUsers_UserId",
                table: "SecurityEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingListItems_Products_ProductId",
                table: "ShoppingListItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingLists_AspNetUsers_UserId",
                table: "ShoppingLists");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLevels_Products_ProductId",
                table: "StockLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLevels_Stores_StoreId",
                table: "StockLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_AspNetUsers_PerformedByUserId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Products_ProductId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_SaleTransactions_RelatedSaleTransactionId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Stores_StoreId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Suppliers_SupplierId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_AspNetUsers_InitiatedByUserId",
                table: "StockTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_Products_ProductId",
                table: "StockTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_Stores_FromStoreId",
                table: "StockTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_Stores_ToStoreId",
                table: "StockTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_StoreCredits_Customers_CustomerId",
                table: "StoreCredits");

            migrationBuilder.DropForeignKey(
                name: "FK_StoreCredits_Stores_StoreId",
                table: "StoreCredits");

            migrationBuilder.DropForeignKey(
                name: "FK_StoreEmployees_AspNetUsers_UserId",
                table: "StoreEmployees");

            migrationBuilder.DropForeignKey(
                name: "FK_StoreEmployees_Stores_StoreId",
                table: "StoreEmployees");

            migrationBuilder.DropForeignKey(
                name: "FK_Stores_AspNetUsers_OwnerUserId",
                table: "Stores");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxRates_Categories_CategoryId",
                table: "TaxRates");

            migrationBuilder.DropForeignKey(
                name: "FK_UserConsents_AspNetUsers_UserId",
                table: "UserConsents");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_AspNetUsers_UserId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserConsents_UserId",
                table: "UserConsents");

            migrationBuilder.DropIndex(
                name: "IX_TaxRates_CategoryId",
                table: "TaxRates");

            migrationBuilder.DropIndex(
                name: "IX_Stores_OwnerUserId",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_StoreEmployees_StoreId",
                table: "StoreEmployees");

            migrationBuilder.DropIndex(
                name: "IX_StoreEmployees_UserId",
                table: "StoreEmployees");

            migrationBuilder.DropIndex(
                name: "IX_StoreCredits_CustomerId",
                table: "StoreCredits");

            migrationBuilder.DropIndex(
                name: "IX_StoreCredits_StoreId",
                table: "StoreCredits");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_FromStoreId",
                table: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_InitiatedByUserId",
                table: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_ProductId",
                table: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_ToStoreId",
                table: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_PerformedByUserId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_RelatedSaleTransactionId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_StoreId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_SupplierId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockLevels_StoreId",
                table: "StockLevels");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingLists_UserId",
                table: "ShoppingLists");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingListItems_ProductId",
                table: "ShoppingListItems");

            migrationBuilder.DropIndex(
                name: "IX_SecurityEvents_UserId",
                table: "SecurityEvents");

            migrationBuilder.DropIndex(
                name: "IX_Scans_ProductId",
                table: "Scans");

            migrationBuilder.DropIndex(
                name: "IX_Scans_StoreId",
                table: "Scans");

            migrationBuilder.DropIndex(
                name: "IX_Scans_UserId",
                table: "Scans");

            migrationBuilder.DropIndex(
                name: "IX_SaleTransactions_CashierUserId",
                table: "SaleTransactions");

            migrationBuilder.DropIndex(
                name: "IX_SaleTransactions_CustomerId",
                table: "SaleTransactions");

            migrationBuilder.DropIndex(
                name: "IX_SaleTransactions_VoidedByUserId",
                table: "SaleTransactions");

            migrationBuilder.DropIndex(
                name: "IX_SaleReturns_ProcessedByUserId",
                table: "SaleReturns");

            migrationBuilder.DropIndex(
                name: "IX_SaleReturns_SaleTransactionId",
                table: "SaleReturns");

            migrationBuilder.DropIndex(
                name: "IX_SaleLineItems_ProductId",
                table: "SaleLineItems");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ProductId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_StoreId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_UserId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_ReviewReplies_ReviewId",
                table: "ReviewReplies");

            migrationBuilder.DropIndex(
                name: "IX_ReviewReplies_StorePartnerUserId",
                table: "ReviewReplies");

            migrationBuilder.DropIndex(
                name: "IX_ReturnLineItems_SaleLineItemId",
                table: "ReturnLineItems");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ProductId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ResolvedByAdminUserId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_StoreId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_UserId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_ReportDisputes_DisputedByUserId",
                table: "ReportDisputes");

            migrationBuilder.DropIndex(
                name: "IX_ReportDisputes_ReportId",
                table: "ReportDisputes");

            migrationBuilder.DropIndex(
                name: "IX_ReorderRules_PreferredSupplierId",
                table: "ReorderRules");

            migrationBuilder.DropIndex(
                name: "IX_ReorderRules_ProductId",
                table: "ReorderRules");

            migrationBuilder.DropIndex(
                name: "IX_ReorderRules_StoreId",
                table: "ReorderRules");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_StoreId",
                table: "Receipts");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_UserId",
                table: "Receipts");

            migrationBuilder.DropIndex(
                name: "IX_ReceiptLineItems_ProductId",
                table: "ReceiptLineItems");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_CreatedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_StoreId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderLineItems_ProductId",
                table: "PurchaseOrderLineItems");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_CategoryId",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_ProductId",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_StoreId",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_ProductSubmissions_BrandId",
                table: "ProductSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_ProductSubmissions_CategoryId",
                table: "ProductSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_ProductSubmissions_ModeratedByAdminUserId",
                table: "ProductSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_ProductSubmissions_SubmittedByUserId",
                table: "ProductSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Products_BrandId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TaxRateId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_ProductBundles_StoreId",
                table: "ProductBundles");

            migrationBuilder.DropIndex(
                name: "IX_ProductBundleItems_ProductId",
                table: "ProductBundleItems");

            migrationBuilder.DropIndex(
                name: "IX_PriceEntryDisputes_DisputedByUserId",
                table: "PriceEntryDisputes");

            migrationBuilder.DropIndex(
                name: "IX_PriceEntryDisputes_PriceEntryId",
                table: "PriceEntryDisputes");

            migrationBuilder.DropIndex(
                name: "IX_PriceEntries_ProductId",
                table: "PriceEntries");

            migrationBuilder.DropIndex(
                name: "IX_PriceEntries_StoreId",
                table: "PriceEntries");

            migrationBuilder.DropIndex(
                name: "IX_PriceEntries_SubmittedByUserId",
                table: "PriceEntries");

            migrationBuilder.DropIndex(
                name: "IX_PriceAlerts_ProductId",
                table: "PriceAlerts");

            migrationBuilder.DropIndex(
                name: "IX_PriceAlerts_UserId",
                table: "PriceAlerts");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SaleTransactionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyTransactions_LoyaltyAccountId",
                table: "LoyaltyTransactions");

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyTransactions_SaleTransactionId",
                table: "LoyaltyTransactions");

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyPrograms_StoreId",
                table: "LoyaltyPrograms");

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyAccounts_CustomerId",
                table: "LoyaltyAccounts");

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyAccounts_LoyaltyProgramId",
                table: "LoyaltyAccounts");

            migrationBuilder.DropIndex(
                name: "IX_FiscalReceipts_SaleTransactionId",
                table: "FiscalReceipts");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_UserId",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_ExpiringOffers_ProductId",
                table: "ExpiringOffers");

            migrationBuilder.DropIndex(
                name: "IX_ExpiringOffers_StoreId",
                table: "ExpiringOffers");

            migrationBuilder.DropIndex(
                name: "IX_DeviceTokens_UserId",
                table: "DeviceTokens");

            migrationBuilder.DropIndex(
                name: "IX_CostPrices_ProductId",
                table: "CostPrices");

            migrationBuilder.DropIndex(
                name: "IX_CostPrices_SetByUserId",
                table: "CostPrices");

            migrationBuilder.DropIndex(
                name: "IX_CostPrices_StoreId",
                table: "CostPrices");

            migrationBuilder.DropIndex(
                name: "IX_ContributorTrustScores_UserId",
                table: "ContributorTrustScores");

            migrationBuilder.DropIndex(
                name: "IX_Commissions_CashierUserId",
                table: "Commissions");

            migrationBuilder.DropIndex(
                name: "IX_Commissions_SaleTransactionId",
                table: "Commissions");

            migrationBuilder.DropIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_CashierShifts_CashierUserId",
                table: "CashierShifts");

            migrationBuilder.DropIndex(
                name: "IX_CashierShifts_StoreId",
                table: "CashierShifts");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_PerformedByUserId",
                table: "AuditLogs");

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance_Amount",
                table: "StoreCredits",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPriceAtSale_Amount",
                table: "SaleLineItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "RefundAmount_Amount",
                table: "ReturnLineItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price_Amount",
                table: "ReceiptLineItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitCost_Amount",
                table: "PurchaseOrderLineItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "BundlePrice_Amount",
                table: "ProductBundles",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price_Amount",
                table: "PriceEntries",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TargetPrice_Amount",
                table: "PriceAlerts",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount_Amount",
                table: "Payments",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance_Amount",
                table: "GiftCards",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "OriginalPrice_Amount",
                table: "ExpiringOffers",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountedPrice_Amount",
                table: "ExpiringOffers",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount_Amount",
                table: "CostPrices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount_Amount",
                table: "Commissions",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "OpeningCash_Amount",
                table: "CashierShifts",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "ExpectedCash_Amount",
                table: "CashierShifts",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ClosingCash_Amount",
                table: "CashierShifts",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
