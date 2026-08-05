using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionsAndRemoveModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSubmissions_AspNetUsers_ModeratedByAdminUserId",
                table: "ProductSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AspNetUsers_ResolvedByAdminUserId",
                table: "Reports");

            migrationBuilder.DropTable(
                name: "PriceEntryDisputes");

            migrationBuilder.DropTable(
                name: "ReportDisputes");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ResolvedByAdminUserId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_ProductSubmissions_ModeratedByAdminUserId",
                table: "ProductSubmissions");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ResolvedByAdminUserId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ModeratedAt",
                table: "ProductSubmissions");

            migrationBuilder.DropColumn(
                name: "ModeratedByAdminUserId",
                table: "ProductSubmissions");

            // NOTE: dropping ProductSubmissions."Status" is deferred to the end of Up() — the data
            // migration below needs to read it (to tell a Pending submission from a Rejected one)
            // before it's gone, and that can only happen once ProductId (added further down) exists
            // to write into.

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveFrom",
                table: "TaxRates",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveTo",
                table: "TaxRates",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConnectedAt",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsVatPayer",
                table: "Stores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StatusChangedAt",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusReason",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxRegime",
                table: "Stores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "ProductSubmissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "PriceEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AfterStateJson",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeforeStateJson",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BlockedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlockedByAdminUserId",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlockedReason",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "AdminInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    CodeHash = table.Column<string>(type: "text", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    InvitedByUserId = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminInvitations_AspNetUsers_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContributorTrustScoreAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Delta = table.Column<double>(type: "double precision", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    IsManual = table.Column<bool>(type: "boolean", nullable: false),
                    PerformedByAdminUserId = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributorTrustScoreAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContributorTrustScoreAdjustments_AspNetUsers_PerformedByAdm~",
                        column: x => x.PerformedByAdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContributorTrustScoreAdjustments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    MaxStores = table.Column<int>(type: "integer", nullable: true),
                    MaxEmployees = table.Column<int>(type: "integer", nullable: true),
                    FeaturesJson = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MonthlyPrice_Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MonthlyPrice_Currency = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreId = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionPlanId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodEndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    PriceAtIssue_Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PriceAtIssue_Currency = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreSubscriptions_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreSubscriptionId = table.Column<int>(type: "integer", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    RecordedByUserId = table.Column<string>(type: "text", nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsReversal = table.Column<bool>(type: "boolean", nullable: false),
                    ReversedPaymentId = table.Column<int>(type: "integer", nullable: true),
                    Amount_Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount_Currency = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionPayments_AspNetUsers_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPayments_StoreSubscriptions_StoreSubscriptionId",
                        column: x => x.StoreSubscriptionId,
                        principalTable: "StoreSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPayments_SubscriptionPayments_ReversedPaymentId",
                        column: x => x.ReversedPaymentId,
                        principalTable: "SubscriptionPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSubmissions_ProductId",
                table: "ProductSubmissions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminInvitations_InvitedByUserId",
                table: "AdminInvitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributorTrustScoreAdjustments_PerformedByAdminUserId",
                table: "ContributorTrustScoreAdjustments",
                column: "PerformedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributorTrustScoreAdjustments_UserId",
                table: "ContributorTrustScoreAdjustments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreSubscriptions_StoreId",
                table: "StoreSubscriptions",
                column: "StoreId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreSubscriptions_SubscriptionPlanId",
                table: "StoreSubscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_RecordedByUserId",
                table: "SubscriptionPayments",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_ReversedPaymentId",
                table: "SubscriptionPayments",
                column: "ReversedPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_StoreSubscriptionId",
                table: "SubscriptionPayments",
                column: "StoreSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Code",
                table: "SubscriptionPlans",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSubmissions_Products_ProductId",
                table: "ProductSubmissions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ── Data migration ──────────────────────────────────────────────────────────────────
            // ADMIN_PROMPT.md §1: "Товары, которые раньше ждали модерации, теперь публикуются
            // сразу. ... переведи все существующие записи в опубликованное состояние миграцией
            // данных." Old ProductSubmissions.Status: 0=Pending, 1=Approved, 2=Rejected.
            //
            // Approved rows already have a matching Product (ModerateNewProductCommandHandler
            // created it at approval time) — just link ProductId to it by barcode.
            migrationBuilder.Sql("""
                UPDATE "ProductSubmissions" ps
                SET "ProductId" = p."Id"
                FROM "Products" p
                WHERE ps."Status" = 1 AND p."Barcode_Value" = ps."Barcode_Value" AND ps."ProductId" IS NULL;
                """);

            // Pending rows never got a Product — create one now (unless a Product with that barcode
            // already exists, e.g. from a different, since-approved submission) so they publish
            // immediately, matching what SubmitNewProductCommandHandler now does for every new
            // submission going forward.
            migrationBuilder.Sql("""
                INSERT INTO "Products" ("Barcode_Value", "Name", "CategoryId", "BrandId", "CountryOfOrigin", "IsSoldByWeight")
                SELECT ps."Barcode_Value", ps."Name", ps."CategoryId", ps."BrandId", ps."CountryOfOrigin", false
                FROM "ProductSubmissions" ps
                WHERE ps."Status" = 0
                AND NOT EXISTS (SELECT 1 FROM "Products" p WHERE p."Barcode_Value" = ps."Barcode_Value");
                """);
            migrationBuilder.Sql("""
                UPDATE "ProductSubmissions" ps
                SET "ProductId" = p."Id"
                FROM "Products" p
                WHERE ps."Status" = 0 AND p."Barcode_Value" = ps."Barcode_Value" AND ps."ProductId" IS NULL;
                """);

            // Rejected rows (Status = 2) are deliberately left with ProductId = NULL — most were
            // rejected as duplicates of a barcode someone else already owns, so linking by barcode
            // here would attach them to the wrong submission's product.

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ProductSubmissions");

            // Backfill the two new NOT NULL timestamp columns for pre-existing rows — EF's own
            // default (DateTimeOffset.MinValue, year 0001) is a clearly-wrong sentinel, not a
            // real value, so every existing row gets a real "now" instead of carrying that forward
            // into admin UI sorting/display.
            migrationBuilder.Sql("""UPDATE "Stores" SET "ConnectedAt" = now() WHERE "ConnectedAt" = '0001-01-01T00:00:00Z';""");
            migrationBuilder.Sql("""UPDATE "AspNetUsers" SET "CreatedAt" = now() WHERE "CreatedAt" = '0001-01-01T00:00:00Z';""");

            // Every existing store already had tax rates applied unconditionally before this
            // feature existed — IsVatPayer defaults to true in the C# model for that reason, but EF
            // migrations don't read property initializers, only what SubscriptionOptions/Fluent API
            // says (false here). Backfill explicitly so behavior doesn't silently change for anyone.
            migrationBuilder.Sql("""UPDATE "Stores" SET "IsVatPayer" = true;""");

            // ADMIN_PROMPT.md §2.1: every Active store should have a subscription — issue a Trial to
            // any that don't yet (a fresh Admin-approved store from before this migration). New
            // approvals go through ApproveStoreCommandHandler instead; this only backfills history.
            migrationBuilder.Sql("""
                INSERT INTO "SubscriptionPlans" ("Name", "Code", "MaxStores", "MaxEmployees", "FeaturesJson", "IsActive", "MonthlyPrice_Amount", "MonthlyPrice_Currency")
                VALUES ('Стандарт', 'standard', NULL, NULL, NULL, true, 200, 'TJS');
                """);
            migrationBuilder.Sql("""
                INSERT INTO "StoreSubscriptions" ("StoreId", "SubscriptionPlanId", "Status", "StartedAt", "CurrentPeriodEndsAt", "Note", "PriceAtIssue_Amount", "PriceAtIssue_Currency")
                SELECT s."Id", (SELECT "Id" FROM "SubscriptionPlans" WHERE "Code" = 'standard'), 0, now(), now() + interval '14 days',
                       'Auto-issued by AddSubscriptionsAndRemoveModeration migration', 200, 'TJS'
                FROM "Stores" s
                WHERE s."Status" = 1
                AND NOT EXISTS (SELECT 1 FROM "StoreSubscriptions" ss WHERE ss."StoreId" = s."Id");
                """);

            // ADMIN_PROMPT.md §2.8: "удали из базы миграцией явно мусорные тестовые бренды и
            // категории, у которых нет ни одного товара и имя не проходит простую проверку
            // осмысленности." Best-effort heuristic, not a guarantee — flagged as such in
            // WORKLOG/the session report, not claimed as exhaustive: unused (zero products/no
            // children/no tax-rate reference) AND the name looks like keyboard-mash/test data
            // (bare lowercase run with no space/punctuation — real brand/category names almost
            // always have a capital letter or a space — or matches an explicit test-word list).
            migrationBuilder.Sql("""
                DELETE FROM "Brands"
                WHERE "Id" NOT IN (SELECT DISTINCT "BrandId" FROM "Products")
                AND (
                    "Name" ~ '^[a-z]{4,12}$'
                    OR "Name" ~* '^(test|asdf|qwerty|xxx+|zzz+|foo|bar|sample|dummy)'
                    OR length(trim("Name")) < 2
                );
                """);
            migrationBuilder.Sql("""
                DELETE FROM "Categories"
                WHERE "Id" NOT IN (SELECT DISTINCT "CategoryId" FROM "Products")
                AND "Id" NOT IN (SELECT DISTINCT "ParentCategoryId" FROM "Categories" WHERE "ParentCategoryId" IS NOT NULL)
                AND "Id" NOT IN (SELECT DISTINCT "CategoryId" FROM "TaxRates" WHERE "CategoryId" IS NOT NULL)
                AND (
                    "Name" ~ '^[a-z]{4,12}$'
                    OR "Name" ~* '^(test|asdf|qwerty|xxx+|zzz+|foo|bar|sample|dummy)'
                    OR length(trim("Name")) < 2
                );
                """);

            // ADMIN_PROMPT.md §2.8: seed a real Tajikistan retail category tree (three levels, ~90
            // nodes) as a one-time data migration — the admin UI (CategoriesPage) is a rare-edit
            // tool afterward (rename/move/reorder/hide), not how ~100 categories get created one by
            // one through a form.
            migrationBuilder.Sql("""
                INSERT INTO "Categories" ("Name", "ParentCategoryId", "DisplayOrder", "IsHidden") VALUES
                ('Продукты питания', NULL, 1, false),
                ('Напитки', NULL, 2, false),
                ('Бытовая химия', NULL, 3, false),
                ('Личная гигиена', NULL, 4, false),
                ('Детские товары', NULL, 5, false),
                ('Хозяйственные товары', NULL, 6, false),
                ('Табачные изделия', NULL, 7, false),
                ('Здоровье и аптека', NULL, 8, false),
                ('Канцелярские и школьные товары', NULL, 9, false),
                ('Текстиль и одежда', NULL, 10, false);
                """);
            migrationBuilder.Sql("""
                INSERT INTO "Categories" ("Name", "ParentCategoryId", "DisplayOrder", "IsHidden")
                SELECT v.child_name, p."Id", v.display_order, false
                FROM (VALUES
                    ('Продукты питания', 'Хлеб и хлебобулочные изделия', 1),
                    ('Продукты питания', 'Молочные продукты и яйца', 2),
                    ('Продукты питания', 'Мясо и птица', 3),
                    ('Продукты питания', 'Рыба и морепродукты', 4),
                    ('Продукты питания', 'Овощи и фрукты', 5),
                    ('Продукты питания', 'Бакалея', 6),
                    ('Продукты питания', 'Консервы', 7),
                    ('Продукты питания', 'Кондитерские изделия', 8),
                    ('Продукты питания', 'Специи и приправы', 9),
                    ('Продукты питания', 'Замороженные продукты', 10),
                    ('Продукты питания', 'Детское питание', 11),
                    ('Продукты питания', 'Растительное и животное масло', 12),
                    ('Напитки', 'Питьевая вода', 1),
                    ('Напитки', 'Соки и нектары', 2),
                    ('Напитки', 'Газированные напитки', 3),
                    ('Напитки', 'Чай', 4),
                    ('Напитки', 'Кофе', 5),
                    ('Напитки', 'Алкогольные напитки', 6),
                    ('Напитки', 'Энергетические напитки', 7),
                    ('Бытовая химия', 'Стиральные порошки и гели', 1),
                    ('Бытовая химия', 'Средства для мытья посуды', 2),
                    ('Бытовая химия', 'Чистящие средства', 3),
                    ('Бытовая химия', 'Освежители воздуха', 4),
                    ('Бытовая химия', 'Средства от насекомых', 5),
                    ('Бытовая химия', 'Пакеты и плёнка для дома', 6),
                    ('Личная гигиена', 'Уход за телом', 1),
                    ('Личная гигиена', 'Уход за волосами', 2),
                    ('Личная гигиена', 'Уход за полостью рта', 3),
                    ('Личная гигиена', 'Средства женской гигиены', 4),
                    ('Личная гигиена', 'Бритвенные принадлежности', 5),
                    ('Личная гигиена', 'Дезодоранты', 6),
                    ('Личная гигиена', 'Уход за кожей лица', 7),
                    ('Детские товары', 'Подгузники', 1),
                    ('Детские товары', 'Товары для новорождённых', 2),
                    ('Детские товары', 'Игрушки', 3),
                    ('Детские товары', 'Детская одежда', 4),
                    ('Детские товары', 'Товары для кормления', 5),
                    ('Детские товары', 'Детская гигиена', 6),
                    ('Хозяйственные товары', 'Посуда и кухонные принадлежности', 1),
                    ('Хозяйственные товары', 'Текстиль для дома', 2),
                    ('Хозяйственные товары', 'Батарейки и электротовары', 3),
                    ('Хозяйственные товары', 'Хозяйственный инвентарь', 4),
                    ('Хозяйственные товары', 'Одноразовая посуда', 5),
                    ('Хозяйственные товары', 'Свечи и спички', 6),
                    ('Табачные изделия', 'Сигареты', 1),
                    ('Табачные изделия', 'Табак и кальянные смеси', 2),
                    ('Табачные изделия', 'Зажигалки', 3),
                    ('Здоровье и аптека', 'Витамины и БАДы', 1),
                    ('Здоровье и аптека', 'Медицинские изделия', 2),
                    ('Здоровье и аптека', 'Средства первой помощи', 3),
                    ('Здоровье и аптека', 'Товары для диабетиков', 4),
                    ('Канцелярские и школьные товары', 'Письменные принадлежности', 1),
                    ('Канцелярские и школьные товары', 'Тетради и бумага', 2),
                    ('Канцелярские и школьные товары', 'Школьные принадлежности', 3),
                    ('Текстиль и одежда', 'Носки и нижнее бельё', 1),
                    ('Текстиль и одежда', 'Головные уборы', 2),
                    ('Текстиль и одежда', 'Перчатки и варежки', 3)
                ) AS v(parent_name, child_name, display_order)
                JOIN "Categories" p ON p."Name" = v.parent_name AND p."ParentCategoryId" IS NULL;
                """);
            migrationBuilder.Sql("""
                INSERT INTO "Categories" ("Name", "ParentCategoryId", "DisplayOrder", "IsHidden")
                SELECT v.grandchild_name, c."Id", v.display_order, false
                FROM (VALUES
                    ('Молочные продукты и яйца', 'Молоко', 1),
                    ('Молочные продукты и яйца', 'Кисломолочные продукты', 2),
                    ('Молочные продукты и яйца', 'Сыры', 3),
                    ('Молочные продукты и яйца', 'Яйца', 4),
                    ('Молочные продукты и яйца', 'Масло и маргарин', 5),
                    ('Мясо и птица', 'Говядина и баранина', 1),
                    ('Мясо и птица', 'Курица', 2),
                    ('Мясо и птица', 'Колбасы и сосиски', 3),
                    ('Мясо и птица', 'Мясные полуфабрикаты', 4),
                    ('Овощи и фрукты', 'Свежие овощи', 1),
                    ('Овощи и фрукты', 'Свежие фрукты', 2),
                    ('Овощи и фрукты', 'Зелень', 3),
                    ('Овощи и фрукты', 'Сухофрукты и орехи', 4),
                    ('Хлеб и хлебобулочные изделия', 'Хлеб', 1),
                    ('Хлеб и хлебобулочные изделия', 'Лаваш и лепёшки', 2),
                    ('Хлеб и хлебобулочные изделия', 'Сдобная выпечка', 3),
                    ('Бакалея', 'Крупы', 1),
                    ('Бакалея', 'Макаронные изделия', 2),
                    ('Бакалея', 'Мука', 3),
                    ('Бакалея', 'Сахар и соль', 4),
                    ('Газированные напитки', 'Кола и тоники', 1),
                    ('Газированные напитки', 'Лимонады', 2)
                ) AS v(parent_name, grandchild_name, display_order)
                JOIN "Categories" c ON c."Name" = v.parent_name AND c."ParentCategoryId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSubmissions_Products_ProductId",
                table: "ProductSubmissions");

            migrationBuilder.DropTable(
                name: "AdminInvitations");

            migrationBuilder.DropTable(
                name: "ContributorTrustScoreAdjustments");

            migrationBuilder.DropTable(
                name: "SubscriptionPayments");

            migrationBuilder.DropTable(
                name: "StoreSubscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_ProductSubmissions_ProductId",
                table: "ProductSubmissions");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "TaxRates");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "TaxRates");

            migrationBuilder.DropColumn(
                name: "ConnectedAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "IsVatPayer",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "StatusChangedAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "StatusReason",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "TaxRegime",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "ProductSubmissions");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "PriceEntries");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "AfterStateJson",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "BeforeStateJson",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "BlockedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BlockedByAdminUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BlockedReason",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResolvedAt",
                table: "Reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedByAdminUserId",
                table: "Reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModeratedAt",
                table: "ProductSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModeratedByAdminUserId",
                table: "ProductSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ProductSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PriceEntryDisputes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DisputedByUserId = table.Column<string>(type: "text", nullable: false),
                    PriceEntryId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceEntryDisputes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceEntryDisputes_AspNetUsers_DisputedByUserId",
                        column: x => x.DisputedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PriceEntryDisputes_PriceEntries_PriceEntryId",
                        column: x => x.PriceEntryId,
                        principalTable: "PriceEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportDisputes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DisputedByUserId = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    ReportId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDisputes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportDisputes_AspNetUsers_DisputedByUserId",
                        column: x => x.DisputedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportDisputes_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ResolvedByAdminUserId",
                table: "Reports",
                column: "ResolvedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSubmissions_ModeratedByAdminUserId",
                table: "ProductSubmissions",
                column: "ModeratedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceEntryDisputes_DisputedByUserId",
                table: "PriceEntryDisputes",
                column: "DisputedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceEntryDisputes_PriceEntryId",
                table: "PriceEntryDisputes",
                column: "PriceEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDisputes_DisputedByUserId",
                table: "ReportDisputes",
                column: "DisputedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDisputes_ReportId",
                table: "ReportDisputes",
                column: "ReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSubmissions_AspNetUsers_ModeratedByAdminUserId",
                table: "ProductSubmissions",
                column: "ModeratedByAdminUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AspNetUsers_ResolvedByAdminUserId",
                table: "Reports",
                column: "ResolvedByAdminUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
