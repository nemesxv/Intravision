using Intravision.Models.Entities;
namespace Intravision.Services.Contracts;

public interface IVendingService
{
    Task<string?> SetChangeModeAsync(CustomerWallet wallet, string? version, bool keepChange, CancellationToken cancellationToken);
    Task<string?> InsertCoinAsync(CustomerWallet wallet, string? version, decimal denomination, CancellationToken cancellationToken);
    Task<string?> PurchaseAsync(CustomerWallet wallet, string? version, int drinkId, string? drinkVersion, CancellationToken cancellationToken);
    Task<string?> ReturnCoinsAsync(CustomerWallet wallet, string? version, CancellationToken cancellationToken);
}
