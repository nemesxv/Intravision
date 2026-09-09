namespace Intravision.Models.Entities;

public class Coin
{
    // The denomination in rubles also uniquely identifies the coin inventory row.
    public decimal Denomination { get; set; }
    public int Quantity { get; set; }
    // Blocking acceptance does not prevent dispensing this denomination as change.
    public bool IsBlocked { get; set; }
    public byte[] RowVersion { get; set; } = [];
}