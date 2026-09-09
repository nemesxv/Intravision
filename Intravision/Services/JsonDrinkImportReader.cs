using System.Text.Json;
using System.Text.Json.Serialization;
using Intravision.Services.Contracts;
namespace Intravision.Services;

public sealed class JsonDrinkImportReader : IDrinkImportReader
{
    public async Task<IReadOnlyList<ImportedDrink?>> ReadAsync(Stream source, CancellationToken cancellationToken)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<List<ImportedDrink?>>(source,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow }, cancellationToken) ?? [];
        }
        catch (JsonException) { throw new ArgumentException("Некорректный JSON. Используйте формат из примера."); }
    }
}
