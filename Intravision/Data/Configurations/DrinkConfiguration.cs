using Intravision.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Intravision.Data.Configurations;

public sealed class DrinkConfiguration : IEntityTypeConfiguration<Drink>
{
    public void Configure(EntityTypeBuilder<Drink> builder)
    {
        builder.ToTable("Drinks", table =>
        {
            table.HasCheckConstraint("CK_Drinks_Price", "[Price] > 0");
            table.HasCheckConstraint("CK_Drinks_Quantity", "[Quantity] >= 0");
            table.HasCheckConstraint("CK_Drinks_Name", "LEN(LTRIM(RTRIM([Name]))) > 0");
        });
        builder.HasKey(drink => drink.Id);
        builder.Property(drink => drink.Price).HasPrecision(10, 2);
        builder.Property(drink => drink.Name).HasMaxLength(100).IsRequired();
        builder.Property(drink => drink.ImagePath).HasMaxLength(500);
        builder.Property(drink => drink.RowVersion).IsRowVersion();
    }
}
