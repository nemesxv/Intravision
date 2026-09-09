using Intravision.Data;
using Intravision.Models;
using Intravision.Models.Entities;
using Intravision.Services.Contracts;
namespace Intravision.Services;

public sealed class DrinkImportService(ApplicationDbContext db, IDrinkImageStore images, IDrinkImportReader reader) : IDrinkImportService
{
    public const int MaxBytes = 20 * 1024 * 1024;
    public async Task<int> ImportAsync(Stream? source, long length, CancellationToken cancellationToken)
    {
        if (source is null || length is <= 0 or > MaxBytes)
            throw new ArgumentException("Выберите JSON-файл размером до 20 МБ.");
        var rows = await reader.ReadAsync(source, cancellationToken);
        if (rows.Count is < 1 or > 50) throw new ArgumentException("Файл должен содержать от 1 до 50 напитков.");
        var prepared = new List<(Drink Drink, byte[] Image)>();
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var prefix = $"Напиток {i + 1}: ";
            if (row is null) throw new ArgumentException(prefix + "пустая запись.");
            var details = new DrinkDetails(row.Name?.Trim() ?? "", row.Price, row.Quantity);
            var errors = DrinkRules.Validate(details).ToArray();
            if (errors.Length > 0) throw new ArgumentException(prefix + string.Join(" ", errors.Select(e => e.Message)));
            byte[] bytes;
            try { bytes = Convert.FromBase64String(row.ImageBase64 ?? ""); }
            catch (FormatException) { throw new ArgumentException(prefix + "изображение должно быть закодировано в Base64."); }
            try { DrinkImageRules.Validate(bytes); }
            catch (ArgumentException exception) { throw new ArgumentException(prefix + exception.Message); }
            prepared.Add((new Drink { Name = details.Name, Price = details.Price, Quantity = details.Quantity }, bytes));
        }
        var createdImages = new List<string>();
        try
        {
            foreach (var item in prepared)
            {
                item.Drink.ImagePath = await images.SaveAsync(item.Image, cancellationToken);
                createdImages.Add(item.Drink.ImagePath);
            }
            db.Drinks.AddRange(prepared.Select(p => p.Drink));
            await db.SaveChangesAsync(cancellationToken);
            return prepared.Count;
        }
        catch
        {
            foreach (var path in createdImages) images.Delete(path);
            throw;
        }
    }
}
