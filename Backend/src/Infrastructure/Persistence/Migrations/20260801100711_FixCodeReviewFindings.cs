using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixCodeReviewFindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLineItems_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderLineItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnLineItems_SaleReturns_SaleReturnId",
                table: "ReturnLineItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleLineItems_SaleTransactions_SaleTransactionId",
                table: "SaleLineItems");

            migrationBuilder.DropIndex(
                name: "IX_StoreEmployees_StoreId",
                table: "StoreEmployees");

            migrationBuilder.DropIndex(
                name: "IX_StoreCredits_StoreId",
                table: "StoreCredits");

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Suppliers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "GiftCardAmountApplied",
                table: "SaleTransactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GiftCardId",
                table: "SaleTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StoreCreditAmountApplied",
                table: "SaleTransactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuedByUserId",
                table: "GiftCards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IssuingStoreId",
                table: "GiftCards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Data backfill for the two new NOT NULL FK columns above — neither Supplier nor
            // GiftCard recorded an owning store before this migration, so existing rows have no
            // real answer. Suppliers get the store of their earliest purchase order where one
            // exists (the closest thing to a real signal); everything else (including every
            // pre-existing GiftCard, which never had any store association at all) falls back to
            // the platform's first-ever store. This is a one-time reassignment of ownership for
            // pre-existing dev/test data, not a decision that should ever run again — see
            // docs/CODE_REVIEW.md SEC-06/SOLID-04 for why these columns exist.
            migrationBuilder.Sql(
                """
                UPDATE "Suppliers" s
                SET "StoreId" = COALESCE(
                    (SELECT po."StoreId" FROM "PurchaseOrders" po WHERE po."SupplierId" = s."Id" ORDER BY po."Id" LIMIT 1),
                    (SELECT MIN("Id") FROM "Stores")
                )
                WHERE EXISTS (SELECT 1 FROM "Stores");
                """);

            migrationBuilder.Sql(
                """
                UPDATE "GiftCards"
                SET "IssuingStoreId" = (SELECT MIN("Id") FROM "Stores")
                WHERE EXISTS (SELECT 1 FROM "Stores");
                """);

            migrationBuilder.CreateTable(
                name: "GiftCardRedemptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GiftCardId = table.Column<int>(type: "integer", nullable: false),
                    StoreId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SaleTransactionId = table.Column<int>(type: "integer", nullable: true),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftCardRedemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftCardRedemptions_GiftCards_GiftCardId",
                        column: x => x.GiftCardId,
                        principalTable: "GiftCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiftCardRedemptions_SaleTransactions_SaleTransactionId",
                        column: x => x.SaleTransactionId,
                        principalTable: "SaleTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GiftCardRedemptions_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreEmployees_StoreId_UserId",
                table: "StoreEmployees",
                columns: new[] { "StoreId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreCredits_StoreId_CustomerId",
                table: "StoreCredits",
                columns: new[] { "StoreId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleTransactions_GiftCardId",
                table: "SaleTransactions",
                column: "GiftCardId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode_Value",
                table: "Products",
                column: "Barcode_Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftCards_IssuingStoreId",
                table: "GiftCards",
                column: "IssuingStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_Token",
                table: "DeviceTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name",
                table: "Brands",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardRedemptions_GiftCardId",
                table: "GiftCardRedemptions",
                column: "GiftCardId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardRedemptions_SaleTransactionId",
                table: "GiftCardRedemptions",
                column: "SaleTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardRedemptions_StoreId",
                table: "GiftCardRedemptions",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_GiftCards_Stores_IssuingStoreId",
                table: "GiftCards",
                column: "IssuingStoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLineItems_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderLineItems",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnLineItems_SaleReturns_SaleReturnId",
                table: "ReturnLineItems",
                column: "SaleReturnId",
                principalTable: "SaleReturns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleLineItems_SaleTransactions_SaleTransactionId",
                table: "SaleLineItems",
                column: "SaleTransactionId",
                principalTable: "SaleTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleTransactions_GiftCards_GiftCardId",
                table: "SaleTransactions",
                column: "GiftCardId",
                principalTable: "GiftCards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GiftCards_Stores_IssuingStoreId",
                table: "GiftCards");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLineItems_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderLineItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnLineItems_SaleReturns_SaleReturnId",
                table: "ReturnLineItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleLineItems_SaleTransactions_SaleTransactionId",
                table: "SaleLineItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleTransactions_GiftCards_GiftCardId",
                table: "SaleTransactions");

            migrationBuilder.DropTable(
                name: "GiftCardRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_StoreEmployees_StoreId_UserId",
                table: "StoreEmployees");

            migrationBuilder.DropIndex(
                name: "IX_StoreCredits_StoreId_CustomerId",
                table: "StoreCredits");

            migrationBuilder.DropIndex(
                name: "IX_SaleTransactions_GiftCardId",
                table: "SaleTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode_Value",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_GiftCards_IssuingStoreId",
                table: "GiftCards");

            migrationBuilder.DropIndex(
                name: "IX_DeviceTokens_Token",
                table: "DeviceTokens");

            migrationBuilder.DropIndex(
                name: "IX_Brands_Name",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "GiftCardAmountApplied",
                table: "SaleTransactions");

            migrationBuilder.DropColumn(
                name: "GiftCardId",
                table: "SaleTransactions");

            migrationBuilder.DropColumn(
                name: "StoreCreditAmountApplied",
                table: "SaleTransactions");

            migrationBuilder.DropColumn(
                name: "IssuedByUserId",
                table: "GiftCards");

            migrationBuilder.DropColumn(
                name: "IssuingStoreId",
                table: "GiftCards");

            migrationBuilder.CreateIndex(
                name: "IX_StoreEmployees_StoreId",
                table: "StoreEmployees",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreCredits_StoreId",
                table: "StoreCredits",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLineItems_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderLineItems",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnLineItems_SaleReturns_SaleReturnId",
                table: "ReturnLineItems",
                column: "SaleReturnId",
                principalTable: "SaleReturns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleLineItems_SaleTransactions_SaleTransactionId",
                table: "SaleLineItems",
                column: "SaleTransactionId",
                principalTable: "SaleTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
