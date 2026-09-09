using Intravision.Data;
using Intravision.Models;
using Intravision.Models.Entities;
using Intravision.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Intravision.Services;

public sealed class VendingService(ApplicationDbContext db, IChangeCalculator changeCalculator) : IVendingService
{
    public Task<string?> SetChangeModeAsync(CustomerWallet wallet, string? version, bool keepChange, CancellationToken cancellationToken) =>
        InTransactionAsync(wallet, version, () =>
        {
            wallet.KeepChange = keepChange;
            return Task.FromResult<string?>(null);
        }, cancellationToken);

    public Task<string?> InsertCoinAsync(CustomerWallet wallet, string? version, decimal denomination, CancellationToken cancellationToken) =>
        InTransactionAsync(wallet, version, async () =>
        {
            var accepted = await db.Coins.SingleOrDefaultAsync(c => c.Denomination == denomination, cancellationToken);
            if (accepted is null || accepted.IsBlocked) return "Прием этой монеты недоступен.";
            var deposits = WalletState.Deposits(wallet);
            if (WalletState.Balance(wallet) + denomination > MoneyLimits.MaxValue)
                return "Достигнут максимальный поддерживаемый баланс: 99 999 999,99 ₽.";
            deposits[denomination] = deposits.GetValueOrDefault(denomination) + 1;
            WalletState.SetDeposits(wallet, deposits);
            WalletState.SetReceipt(wallet, null);
            return null;
        }, cancellationToken);

    public Task<string?> ReturnCoinsAsync(CustomerWallet wallet, string? version, CancellationToken cancellationToken) =>
        InTransactionAsync(wallet, version, () =>
        {
            var balance = WalletState.Balance(wallet);
            if (balance == 0) return Task.FromResult<string?>("Нет монет для возврата.");
            WalletState.SetReceipt(wallet, new("Монеты возвращены. Баланс выдан полностью.", balance, WalletState.Deposits(wallet)));
            WalletState.SetDeposits(wallet, []);
            return Task.FromResult<string?>(null);
        }, cancellationToken);

    public Task<string?> PurchaseAsync(CustomerWallet wallet, string? version, int drinkId, string? drinkVersion, CancellationToken cancellationToken) =>
        InTransactionAsync(wallet, version, async () =>
        {
            var drink = await db.Drinks.SingleOrDefaultAsync(d => d.Id == drinkId, cancellationToken);
            if (drink is null || drink.Quantity == 0) return "Этот напиток закончился или был удален.";
            if (drinkVersion != Convert.ToBase64String(drink.RowVersion))
                return "Данные напитка изменились. Проверьте цену и выберите напиток снова.";
            var balance = WalletState.Balance(wallet);
            if (drink.Price > balance) return "Недостаточно средств для покупки.";
            var deposits = WalletState.Deposits(wallet);
            var coins = await db.Coins.OrderBy(c => c.Denomination).ToListAsync(cancellationToken);
            var available = new Dictionary<decimal, int>();
            foreach (var coin in coins)
            {
                var total = (long)coin.Quantity + deposits.GetValueOrDefault(coin.Denomination);
                if (total > int.MaxValue) return "Автомат переполнен монетами. Верните внесенные монеты.";
                available[coin.Denomination] = (int)total;
            }
            var change = changeCalculator.Calculate(balance - drink.Price, available);
            if (change is null) return "Автомат не может выдать точную сдачу. Выберите другой напиток или верните монеты.";
            foreach (var coin in coins)
                coin.Quantity = available[coin.Denomination] - change.GetValueOrDefault(coin.Denomination);
            drink.Quantity--;
            // Remaining coins stay reserved outside machine inventory until collected.
            WalletState.SetDeposits(wallet, wallet.KeepChange ? change : []);
            WalletState.SetReceipt(wallet, new($"Ваш напиток: {drink.Name}. Спасибо за покупку!",
                wallet.KeepChange ? 0 : balance - drink.Price, wallet.KeepChange ? new() : change,
                wallet.KeepChange ? balance - drink.Price : 0));
            return null;
        }, cancellationToken);

    private async Task<string?> InTransactionAsync(CustomerWallet wallet, string? version,
        Func<Task<string?>> operation, CancellationToken cancellationToken)
    {
        if (version != Convert.ToBase64String(wallet.RowVersion))
            return "Страница устарела или действие уже выполнено. Проверьте текущий баланс.";
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var error = await operation();
        if (error is not null) return error;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            return "Состояние автомата изменилось. Проверьте баланс и повторите действие.";
        }
    }
}
