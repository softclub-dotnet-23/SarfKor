using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreApprovalAndOwnerInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Stores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Every store that existed before "approval" was a concept was, de facto, already
            // approved — leaving them at Pending (0, EF's scaffolded default) would silently remove
            // them from every consumer-facing surface (ScanBarcode / CompareStores / ExpiringOffers)
            // the moment this migration runs.
            migrationBuilder.Sql("""UPDATE "Stores" SET "Status" = 1;""");

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlySalary_Amount",
                table: "StoreEmployees",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MonthlySalary_Currency",
                table: "StoreEmployees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ScheduleEnd",
                table: "StoreEmployees",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ScheduleStart",
                table: "StoreEmployees",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StoreOwnerInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    StoreName = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    CodeHash = table.Column<string>(type: "text", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    InvitedByUserId = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Location_Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Location_Longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreOwnerInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreOwnerInvitations_AspNetUsers_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreOwnerInvitations_Email",
                table: "StoreOwnerInvitations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_StoreOwnerInvitations_InvitedByUserId",
                table: "StoreOwnerInvitations",
                column: "InvitedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreOwnerInvitations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "MonthlySalary_Amount",
                table: "StoreEmployees");

            migrationBuilder.DropColumn(
                name: "MonthlySalary_Currency",
                table: "StoreEmployees");

            migrationBuilder.DropColumn(
                name: "ScheduleEnd",
                table: "StoreEmployees");

            migrationBuilder.DropColumn(
                name: "ScheduleStart",
                table: "StoreEmployees");
        }
    }
}
