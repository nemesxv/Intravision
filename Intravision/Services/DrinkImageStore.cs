using Intravision.Services.Contracts;
namespace Intravision.Services;

public sealed class DrinkImageStore(IWebHostEnvironment environment) : IDrinkImageStore
{
    public async Task<string> SaveAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        var extension = DrinkImageRules.Validate(bytes);
        var directory = Path.Combine(environment.WebRootPath, "images", "drinks");
        Directory.CreateDirectory(directory);
        var name = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(directory, name);
        try { await File.WriteAllBytesAsync(path, bytes, cancellationToken); }
        catch { File.Delete(path); throw; }
        return $"/images/drinks/{name}";
    }

    public void Delete(string? path)
    {
        if (path is null || !path.StartsWith("/images/drinks/", StringComparison.Ordinal)) return;
        var name = path["/images/drinks/".Length..];
        if (Path.GetFileName(name) != name || !Guid.TryParseExact(Path.GetFileNameWithoutExtension(name), "N", out _)) return;
        File.Delete(Path.Combine(environment.WebRootPath, "images", "drinks", name));
    }
}
