namespace Intravision.Models.Entities;

public class CustomerWallet
{
    public Guid Id { get; set; }
    // Coins remain in escrow until a purchase, so cancellation can return them exactly.
    public string DepositsJson { get; set; } = "{}";
    public bool KeepChange { get; set; }
    public string? ReceiptJson { get; set; }
    public byte[] RowVersion { get; set; } = [];
}