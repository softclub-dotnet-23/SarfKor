using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeStoreEmployeeInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "StoreId",
                table: "StoreEmployeeInvitations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "StoreEmployeeInvitations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            // Backfilled 'StorePartner' for every pre-existing row — every one of them was, until
            // now, necessarily a store-employee (Owner/Cashier) invite, the only kind this table
            // used to represent.
            migrationBuilder.AddColumn<string>(
                name: "InvitedRole",
                table: "StoreEmployeeInvitations",
                type: "text",
                nullable: false,
                defaultValue: "StorePartner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvitedRole",
                table: "StoreEmployeeInvitations");

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "StoreEmployeeInvitations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StoreId",
                table: "StoreEmployeeInvitations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
