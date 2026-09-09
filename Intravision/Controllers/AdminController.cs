using Intravision.Filters;
using Intravision.Services;
using Intravision.Services.Contracts;
using Intravision.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
namespace Intravision.Controllers;

[ServiceFilter(typeof(AdminKeyFilter), Order = -2000)]
[ServiceFilter(typeof(ApiAntiforgeryFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AdminController(ICatalogQueries catalog, IAdminService admin, IDrinkImportService importer, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("/api/admin")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => Ok(new {
        drinks = await catalog.GetDrinksAsync(cancellationToken), coins = await catalog.GetCoinsAsync(cancellationToken),
        token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken
    });

    [HttpPost("/api/admin/drinks/import")]
    [RequestSizeLimit(DrinkImportService.MaxBytes + 65536)]
    public async Task<IActionResult> Import(IFormFile? importFile, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = importFile?.OpenReadStream();
            var count = await importer.ImportAsync(stream, importFile?.Length ?? 0, cancellationToken);
            return Ok(new { message = $"Импортировано напитков: {count}." });
        }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost("/api/admin/drinks/save")]
    [RequestSizeLimit(DrinkImageRules.MaxBytes + 65536)]
    public async Task<IActionResult> SaveDrink([Bind(Prefix = "Drink")] DrinkInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        byte[]? image = null;
        if (input.Image is not null)
        {
            if (input.Image.Length is <= 0 or > DrinkImageRules.MaxBytes)
            {
                ModelState.AddModelError("Drink.Image", "Изображение должно быть размером от 1 байта до 2 МБ.");
                return ValidationProblem(ModelState);
            }
            await using var source = input.Image.OpenReadStream();
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer, cancellationToken);
            image = buffer.ToArray();
        }
        var result = await admin.SaveDrinkAsync(new(input.Id, input.ToDetails(), input.RowVersion, image), cancellationToken);
        return Respond(result, "Напиток сохранен.");
    }

    [HttpPost("/api/admin/drinks/delete")]
    public async Task<IActionResult> DeleteDrink(int id, string? rowVersion, CancellationToken cancellationToken)
    {
        var result = await admin.DeleteDrinkAsync(id, rowVersion, cancellationToken);
        if (result.Status == AdminStatus.Invalid) return BadRequest(new { error = "Некорректная версия записи." });
        return Respond(result, "Напиток удален.");
    }

    [HttpPost("/api/admin/coins/save")]
    public async Task<IActionResult> SaveCoins(CoinSettingsInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var commands = input.Coins.Select(c => new SaveCoinCommand(c.Denomination, c.Quantity, c.IsBlocked, c.RowVersion)).ToArray();
        var result = await admin.SaveCoinsAsync(commands, cancellationToken);
        if (result.Status == AdminStatus.Invalid)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(error.Field, error.Message);
            return ValidationProblem(ModelState);
        }
        return Respond(result, "Настройки монет сохранены.");
    }

    private IActionResult Respond(AdminResult result, string success) => result.Status switch
    {
        AdminStatus.NotFound => NotFound(new { error = "Напиток уже удалён." }),
        AdminStatus.Conflict => Conflict(new { error = "Данные уже изменены. Обновите страницу и повторите изменение." }),
        AdminStatus.Invalid => BadRequest(new { error = string.Join(" ", result.Errors.Select(e => e.Message)) }),
        _ => Ok(new { message = success })
    };
}
