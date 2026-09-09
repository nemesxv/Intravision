using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Intravision.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDrinksAndCoins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Coins",
                columns: table => new
                {
                    Denomination = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coins", x => x.Denomination);
                    table.CheckConstraint("CK_Coins_Denomination", "[Denomination] IN (1, 2, 5, 10)");
                    table.CheckConstraint("CK_Coins_Quantity", "[Quantity] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Drinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drinks", x => x.Id);
                    table.CheckConstraint("CK_Drinks_ImagePath", "LEN(LTRIM(RTRIM([ImagePath]))) > 0");
                    table.CheckConstraint("CK_Drinks_Name", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Drinks_Price", "[Price] > 0");
                    table.CheckConstraint("CK_Drinks_Quantity", "[Quantity] >= 0");
                });

            migrationBuilder.InsertData(
                table: "Coins",
                columns: new[] { "Denomination", "IsBlocked", "Quantity" },
                values: new object[,]
                {
                    { 1, false, 0 },
                    { 2, false, 0 },
                    { 5, false, 0 },
                    { 10, false, 0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Coins");

            migrationBuilder.DropTable(
                name: "Drinks");
        }
    }
}
