using Intravision.Data;
using Intravision.Models.Entities;
using Intravision.Services.Contracts;
using Microsoft.EntityFrameworkCore;
namespace Intravision.Services;
public sealed class CatalogQueries(ApplicationDbContext db) : ICatalogQueries
{
    public Task<List<Drink>> GetDrinksAsync(CancellationToken cancellationToken) =>
        db.Drinks.AsNoTracking().OrderBy(d => d.Name).ToListAsync(cancellationToken);
    public Task<List<Coin>> GetCoinsAsync(CancellationToken cancellationToken) =>
        db.Coins.AsNoTracking().OrderBy(c => c.Denomination).ToListAsync(cancellationToken);
    public Task<Drink?> GetDrinkAsync(int id, CancellationToken cancellationToken) =>
        db.Drinks.AsNoTracking().SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
}
