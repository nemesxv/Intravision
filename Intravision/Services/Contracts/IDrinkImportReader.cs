namespace Intravision.Services.Contracts;
public interface IDrinkImportReader
{
    Task<IReadOnlyList<ImportedDrink?>> ReadAsync(Stream source, CancellationToken cancellationToken);
}

public sealed class ImportedDrink
{
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public required int Quantity { get; init; }
    public required string ImageBase64 { get; init; }
}
