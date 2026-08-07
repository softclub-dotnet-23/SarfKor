using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReworkStoreEmployeeInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoreEmployeeInvitations_StoreId",
                table: "StoreEmployeeInvitations");

            migrationBuilder.RenameColumn(
                name: "Token",
                table: "StoreEmployeeInvitations",
                newName: "TokenHash");

            migrationBuilder.RenameIndex(
                name: "IX_StoreEmployeeInvitations_Token",
                table: "StoreEmployeeInvitations",
                newName: "IX_StoreEmployeeInvitations_TokenHash");

            migrationBuilder.AddColumn<string>(
                name: "AcceptedUserId",
                table: "StoreEmployeeInvitations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSentAt",
                table: "StoreEmployeeInvitations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "StoreEmployeeInvitations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StoreEmployeeInvitations_StoreId_Email_Status",
                table: "StoreEmployeeInvitations",
                columns: new[] { "StoreId", "Email", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoreEmployeeInvitations_StoreId_Email_Status",
                table: "StoreEmployeeInvitations");

            migrationBuilder.DropColumn(
                name: "AcceptedUserId",
                table: "StoreEmployeeInvitations");

            migrationBuilder.DropColumn(
                name: "LastSentAt",
                table: "StoreEmployeeInvitations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "StoreEmployeeInvitations");

            migrationBuilder.RenameColumn(
                name: "TokenHash",
                table: "StoreEmployeeInvitations",
                newName: "Token");

            migrationBuilder.RenameIndex(
                name: "IX_StoreEmployeeInvitations_TokenHash",
                table: "StoreEmployeeInvitations",
                newName: "IX_StoreEmployeeInvitations_Token");

            migrationBuilder.CreateIndex(
                name: "IX_StoreEmployeeInvitations_StoreId",
                table: "StoreEmployeeInvitations",
                column: "StoreId");
        }
    }
}
