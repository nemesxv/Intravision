using Intravision.Data;
using Intravision.Models;
using Intravision.Models.Entities;
using Intravision.Services.Contracts;
using Microsoft.EntityFrameworkCore;
namespace Intravision.Services;

public sealed class AdminService(ApplicationDbContext db, IDrinkImageStore images) : IAdminService
{
    public async Task<AdminResult> SaveDrinkAsync(SaveDrinkCommand command, CancellationToken cancellationToken)
    {
        var details = command.Details with { Name = command.Details.Name?.Trim() ?? "" };
        var errors = DrinkRules.Validate(details).ToList();
        if (command.Id < 0) errors.Add(new("Id", "Некорректный напиток."));
        if (errors.Count > 0) return AdminResult.Invalid(errors.ToArray());
        var drink = command.Id == 0 ? new Drink() : await db.Drinks.FindAsync([command.Id], cancellationToken);
        if (drink is null) return AdminResult.Missing();
        if (command.Id != 0 && !SetVersion(drink, command.RowVersion)) return InvalidVersion();
        string? newImage = null;
        try
        {
            if (command.Image is not null) newImage = await images.SaveAsync(command.Image, cancellationToken);
        }
        catch (ArgumentException exception) { return AdminResult.Invalid(new ValidationIssue("Image", exception.Message)); }
        var oldImage = drink.ImagePath;
        drink.Name = details.Name;
        drink.Price = details.Price;
        drink.Quantity = details.Quantity;
        if (newImage is not null) drink.ImagePath = newImage;
        if (command.Id == 0) db.Drinks.Add(drink);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            images.Delete(newImage);
            db.ChangeTracker.Clear();
            return AdminResult.Conflict();
        }
        catch { images.Delete(newImage); throw; }
        if (newImage is not null) images.Delete(oldImage);
        return AdminResult.Success();
    }

    public async Task<AdminResult> DeleteDrinkAsync(int id, string? rowVersion, CancellationToken cancellationToken)
    {
        var drink = await db.Drinks.FindAsync([id], cancellationToken);
        if (drink is null) return AdminResult.Missing();
        if (!SetVersion(drink, rowVersion)) return InvalidVersion();
        db.Drinks.Remove(drink);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return AdminResult.Conflict(); }
        images.Delete(drink.ImagePath);
        return AdminResult.Success();
    }

    public async Task<AdminResult> SaveCoinsAsync(IReadOnlyList<SaveCoinCommand> commands, CancellationToken cancellationToken)
    {
        var coins = await db.Coins.OrderBy(c => c.Denomination).ToListAsync(cancellationToken);
        if (commands.Count != coins.Count || commands.Select(c => c.Denomination).Distinct().Count() != commands.Count ||
            !commands.Select(c => c.Denomination).Order().SequenceEqual(coins.Select(c => c.Denomination)))
            return AdminResult.Invalid(new ValidationIssue("", "Передайте настройки всех номиналов без повторений."));
        for (var i = 0; i < commands.Count; i++)
        {
            var command = commands[i];
            if (command.Quantity < 0)
                return AdminResult.Invalid(new ValidationIssue($"Coins[{i}].Quantity", "Количество монет не может быть отрицательным."));
            var coin = coins.Single(c => c.Denomination == command.Denomination);
            if (!SetVersion(coin, command.RowVersion)) return InvalidVersion();
            if (command.RowVersion != Convert.ToBase64String(coin.RowVersion)) return AdminResult.Conflict();
        }
        foreach (var command in commands)
        {
            var coin = coins.Single(c => c.Denomination == command.Denomination);
            coin.Quantity = command.Quantity;
            coin.IsBlocked = command.IsBlocked;
        }
        // A single SaveChanges transaction commits all four settings or none.
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return AdminResult.Conflict(); }
        return AdminResult.Success();
    }

    private bool SetVersion(object entity, string? version)
    {
        try
        {
            var bytes = Convert.FromBase64String(version ?? "");
            if (bytes.Length != 8) return false;
            db.Entry(entity).Property("RowVersion").OriginalValue = bytes;
            return true;
        }
        catch (FormatException) { return false; }
    }
    private static AdminResult InvalidVersion() => AdminResult.Invalid(new ValidationIssue("", "Некорректная версия записи. Обновите страницу."));
}
