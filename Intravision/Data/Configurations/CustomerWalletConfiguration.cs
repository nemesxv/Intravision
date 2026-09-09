using Intravision.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Intravision.Data.Configurations;

public sealed class CustomerWalletConfiguration : IEntityTypeConfiguration<CustomerWallet>
{
    public void Configure(EntityTypeBuilder<CustomerWallet> builder)
    {
        builder.ToTable("CustomerWallets");
        builder.HasKey(wallet => wallet.Id);
        builder.Property(wallet => wallet.Id).ValueGeneratedNever();
        builder.Property(wallet => wallet.DepositsJson).HasMaxLength(4000).IsRequired();
        builder.Property(wallet => wallet.ReceiptJson).HasMaxLength(4000);
        builder.Property(wallet => wallet.RowVersion).IsRowVersion();
    }
}