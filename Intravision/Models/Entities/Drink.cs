namespace Intravision.Models.Entities;

public class Drink
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
