using Intravision.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Intravision.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<CustomerWallet> CustomerWallets => Set<CustomerWallet>();
    public DbSet<Drink> Drinks => Set<Drink>();
    public DbSet<Coin> Coins => Set<Coin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}