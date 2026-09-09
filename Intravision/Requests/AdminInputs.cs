using System.ComponentModel.DataAnnotations;
using Intravision.Models;
namespace Intravision.Requests;

public class DrinkInput : IValidatableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? RowVersion { get; set; }
    public IFormFile? Image { get; set; }
    public DrinkDetails ToDetails() => new(Name?.Trim() ?? "", Price, Quantity);
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var error in DrinkRules.Validate(ToDetails()))
            yield return new(error.Message, [error.Field]);
        if (Id < 0) yield return new("Некорректный напиток.", [nameof(Id)]);
    }
}
public class CoinInput
{
    public decimal Denomination { get; set; }
    [Range(0, int.MaxValue, ErrorMessage = "Количество монет не может быть отрицательным.")]
    public int Quantity { get; set; }
    public bool IsBlocked { get; set; }
    [Required]
    public string RowVersion { get; set; } = "";
}

public class CoinSettingsInput
{
    public List<CoinInput> Coins { get; set; } = [];
}
