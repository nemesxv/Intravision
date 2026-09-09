using Intravision.Models.Entities;
namespace Intravision.Services.Contracts;
public interface ICatalogQueries
{
    Task<List<Drink>> GetDrinksAsync(CancellationToken cancellationToken);
    Task<List<Coin>> GetCoinsAsync(CancellationToken cancellationToken);
    Task<Drink?> GetDrinkAsync(int id, CancellationToken cancellationToken);
}
