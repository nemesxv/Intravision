using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intravision.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeDrinkImageOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Drinks_ImagePath",
                table: "Drinks");

            migrationBuilder.AlterColumn<string>(
                name: "ImagePath",
                table: "Drinks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Drinks] SET [ImagePath] = '/images/drinks/missing' WHERE [ImagePath] IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "ImagePath",
                table: "Drinks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "/images/drinks/missing",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Drinks_ImagePath",
                table: "Drinks",
                sql: "LEN(LTRIM(RTRIM([ImagePath]))) > 0");
        }
    }
}
