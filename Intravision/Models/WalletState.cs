using System.Text.Json;
using Intravision.Models.Entities;

namespace Intravision.Models;

public record VendingReceipt(string Message, decimal ReturnedAmount, Dictionary<decimal, int> Coins, decimal RetainedAmount = 0);

public static class WalletState
{
    public static Dictionary<decimal, int> Deposits(CustomerWallet wallet) =>
        JsonSerializer.Deserialize<Dictionary<decimal, int>>(wallet.DepositsJson)!;
    public static decimal Balance(CustomerWallet wallet) => Deposits(wallet).Sum(c => c.Key * c.Value);
    public static VendingReceipt? Receipt(CustomerWallet wallet) =>
        wallet.ReceiptJson is null ? null : JsonSerializer.Deserialize<VendingReceipt>(wallet.ReceiptJson);
    public static void SetDeposits(CustomerWallet wallet, Dictionary<decimal, int> coins) =>
        wallet.DepositsJson = JsonSerializer.Serialize(coins);
    public static void SetReceipt(CustomerWallet wallet, VendingReceipt? receipt) =>
        wallet.ReceiptJson = receipt is null ? null : JsonSerializer.Serialize(receipt);
}
