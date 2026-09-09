namespace Intravision.Services.Contracts;
public interface IDrinkImageStore
{
    Task<string> SaveAsync(byte[] bytes, CancellationToken cancellationToken);
    void Delete(string? path);
}
