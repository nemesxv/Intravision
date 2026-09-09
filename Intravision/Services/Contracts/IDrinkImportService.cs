namespace Intravision.Services.Contracts;
public interface IDrinkImportService
{
    Task<int> ImportAsync(Stream? source, long length, CancellationToken cancellationToken);
}
