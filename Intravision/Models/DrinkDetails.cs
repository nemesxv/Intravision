namespace Intravision.Models;

public record DrinkDetails(string Name, decimal Price, int Quantity);
public record ValidationIssue(string Field, string Message);

public static class MoneyLimits
{
    public const decimal MaxValue = 99999999.99m;
}

public static class DrinkRules
{
    public static IEnumerable<ValidationIssue> Validate(DrinkDetails details)
    {
        if (string.IsNullOrWhiteSpace(details.Name))
            yield return new(nameof(details.Name), "Введите название.");
        else if (details.Name.Length > 100)
            yield return new(nameof(details.Name), "Название: не более 100 символов.");
        if (details.Price <= 0 || details.Price > MoneyLimits.MaxValue)
            yield return new(nameof(details.Price), "Цена должна быть от 0,01 до 99 999 999,99 ₽.");
        if (details.Price != decimal.Round(details.Price, 2))
            yield return new(nameof(details.Price), "Укажите цену с точностью до копеек.");
        if (details.Quantity < 0)
            yield return new(nameof(details.Quantity), "Количество не может быть отрицательным.");
    }
}
