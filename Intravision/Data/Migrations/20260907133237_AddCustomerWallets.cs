using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intravision.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerWallets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerWallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepositsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ReceiptJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerWallets", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerWallets");
        }
    }
}
