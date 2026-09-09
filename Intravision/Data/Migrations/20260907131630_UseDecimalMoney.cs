using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intravision.Data.Migrations;

public partial class UseDecimalMoney : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Preserve inventory rows while changing the primary-key column type.
        migrationBuilder.Sql("""
            ALTER TABLE [Coins] DROP CONSTRAINT [PK_Coins];
            ALTER TABLE [Coins] DROP CONSTRAINT [CK_Coins_Denomination];
            ALTER TABLE [Drinks] DROP CONSTRAINT [CK_Drinks_Price];
            ALTER TABLE [Coins] ALTER COLUMN [Denomination] decimal(10,2) NOT NULL;
            ALTER TABLE [Drinks] ALTER COLUMN [Price] decimal(10,2) NOT NULL;
            ALTER TABLE [Coins] ADD CONSTRAINT [PK_Coins] PRIMARY KEY ([Denomination]);
            ALTER TABLE [Coins] ADD CONSTRAINT [CK_Coins_Denomination] CHECK ([Denomination] IN (1, 2, 5, 10));
            ALTER TABLE [Drinks] ADD CONSTRAINT [CK_Drinks_Price] CHECK ([Price] > 0);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM [Drinks] WHERE [Price] <> FLOOR([Price]))
                OR EXISTS (SELECT 1 FROM [Coins] WHERE [Denomination] <> FLOOR([Denomination]))
                THROW 50001, 'Cannot revert money to integers while fractional values exist.', 1;
            ALTER TABLE [Coins] DROP CONSTRAINT [PK_Coins];
            ALTER TABLE [Coins] DROP CONSTRAINT [CK_Coins_Denomination];
            ALTER TABLE [Drinks] DROP CONSTRAINT [CK_Drinks_Price];
            ALTER TABLE [Coins] ALTER COLUMN [Denomination] int NOT NULL;
            ALTER TABLE [Drinks] ALTER COLUMN [Price] int NOT NULL;
            ALTER TABLE [Coins] ADD CONSTRAINT [PK_Coins] PRIMARY KEY ([Denomination]);
            ALTER TABLE [Coins] ADD CONSTRAINT [CK_Coins_Denomination] CHECK ([Denomination] IN (1, 2, 5, 10));
            ALTER TABLE [Drinks] ADD CONSTRAINT [CK_Drinks_Price] CHECK ([Price] > 0);
            """);
    }
}