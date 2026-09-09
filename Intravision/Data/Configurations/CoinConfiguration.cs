using Intravision.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Intravision.Data.Configurations;

public sealed class CoinConfiguration : IEntityTypeConfiguration<Coin>
{
    public void Configure(EntityTypeBuilder<Coin> builder)
    {
        builder.ToTable("Coins", table =>
        {
            table.HasCheckConstraint("CK_Coins_Denomination", "[Denomination] IN (1, 2, 5, 10)");
            table.HasCheckConstraint("CK_Coins_Quantity", "[Quantity] >= 0");
        });
        builder.HasKey(coin => coin.Denomination);
        builder.Property(coin => coin.Denomination).HasPrecision(10, 2).ValueGeneratedNever();
        builder.Property(coin => coin.RowVersion).IsRowVersion();
        builder.HasData(
            new { Denomination = 1m, Quantity = 0, IsBlocked = false },
            new { Denomination = 2m, Quantity = 0, IsBlocked = false },
            new { Denomination = 5m, Quantity = 0, IsBlocked = false },
            new { Denomination = 10m, Quantity = 0, IsBlocked = false });
    }
}