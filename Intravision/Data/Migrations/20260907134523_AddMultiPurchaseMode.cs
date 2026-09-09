using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intravision.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiPurchaseMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "KeepChange",
                table: "CustomerWallets",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KeepChange",
                table: "CustomerWallets");
        }
    }
}
