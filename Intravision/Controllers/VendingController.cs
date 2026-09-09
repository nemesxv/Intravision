using Intravision.Filters;
using Intravision.Models;
using Intravision.Models.Entities;
using Intravision.Services;
using Intravision.Services.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
namespace Intravision.Controllers;

[ServiceFilter(typeof(ApiAntiforgeryFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class VendingController(ICatalogQueries catalog, CustomerWalletAccessor wallets, IVendingService vending, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("/api/vending")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var wallet = (await wallets.GetAsync(HttpContext, true, ct))!;
        return Ok(new { drinks = await catalog.GetDrinksAsync(ct), coins = await catalog.GetCoinsAsync(ct),
            wallet.KeepChange, balance = WalletState.Balance(wallet), version = Convert.ToBase64String(wallet.RowVersion),
            receipt = WalletState.Receipt(wallet), token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }
    [HttpPost("/api/vending/mode")]
    public Task<IActionResult> Mode(bool keepChange, string? version, CancellationToken ct) =>
        Run(w => vending.SetChangeModeAsync(w, version, keepChange, ct), ct);
    [HttpPost("/api/vending/insert")]
    public Task<IActionResult> Insert(decimal denomination, string? version, CancellationToken ct) =>
        Run(w => vending.InsertCoinAsync(w, version, denomination, ct), ct);
    [HttpPost("/api/vending/purchase")]
    public Task<IActionResult> Purchase(int drinkId, string? version, string? drinkVersion, CancellationToken ct) =>
        Run(w => vending.PurchaseAsync(w, version, drinkId, drinkVersion, ct), ct);
    [HttpPost("/api/vending/return")]
    public Task<IActionResult> Return(string? version, CancellationToken ct) =>
        Run(w => vending.ReturnCoinsAsync(w, version, ct), ct);
    private async Task<IActionResult> Run(Func<CustomerWallet, Task<string?>> operation, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var wallet = await wallets.GetAsync(HttpContext, false, ct);
        if (wallet is null) return BadRequest(new { error = "Обновите страницу и разрешите cookies." });
        var error = await operation(wallet);
        return error is null ? Ok(new { success = true }) : Conflict(new { error });
    }
}
