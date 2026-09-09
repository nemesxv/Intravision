using Intravision.Data;
using Intravision.Models;
using Intravision.Services;
using Intravision.Services.Contracts;
using Microsoft.EntityFrameworkCore;

var png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aD1sAAAAASUVORK5CYII=";
ImportedDrink Row(int quantity = 1) => new() { Name = "Drink", Price = 12.50m, Quantity = quantity, ImageBase64 = png };
async Task CheckFailure(IReadOnlyList<ImportedDrink?> rows, bool failDatabase, int failImage, int expectedDeletes, Type expectedException)
{
    await using var db = new TestDb(failDatabase);
    var store = new TestImages(failImage);
    IDrinkImportService service = new DrinkImportService(db, store, new TestReader(rows));
    using var source = new MemoryStream([1]);
    try { await service.ImportAsync(source, source.Length, default); throw new Exception("Expected import failure"); }
    catch (Exception error) when (error.GetType() == expectedException) { }
    if (store.Deleted.Count != expectedDeletes || store.Deleted.Except(store.Saved).Any())
        throw new Exception("Created-image cleanup failed");
    if (expectedDeletes == 0 && (store.Saved.Count != 0 || db.SaveCount != 0))
        throw new Exception("Invalid batch performed side effects");
}
await CheckFailure([Row(), Row(-1)], false, 0, 0, typeof(ArgumentException));
await CheckFailure([Row(), Row()], false, 2, 1, typeof(IOException));
await CheckFailure([Row(), Row()], true, 0, 2, typeof(DbUpdateException));
await using (var db = new TestDb(false))
{
    var images = new TestImages(0);
    var service = new DrinkImportService(db, images, new TestReader([Row(), Row()]));
    using var source = new MemoryStream([1]);
    if (await service.ImportAsync(source, 1, default) != 2 || images.Saved.Count != 2 || images.Deleted.Count != 0 || db.SaveCount != 1)
        throw new Exception("Successful import contract failed");
}
if (!DrinkRules.Validate(new(" ", 0m, -1)).Select(e => e.Field).ToHashSet().SetEquals(["Name", "Price", "Quantity"]))
    throw new Exception("Shared field validation failed");
Console.WriteLine("PASS: replaceable import reader/storage, validation before side effects, image-failure cleanup, database-failure cleanup, shared drink rules.");

sealed class TestReader(IReadOnlyList<ImportedDrink?> rows) : IDrinkImportReader
{
    public Task<IReadOnlyList<ImportedDrink?>> ReadAsync(Stream source, CancellationToken cancellationToken) => Task.FromResult(rows);
}
sealed class TestImages(int failOnWrite) : IDrinkImageStore
{
    public List<string> Saved { get; } = [];
    public List<string> Deleted { get; } = [];
    public Task<string> SaveAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        if (Saved.Count + 1 == failOnWrite) throw new IOException("Simulated image storage failure");
        var path = $"/images/drinks/{Guid.NewGuid():N}.png";
        Saved.Add(path);
        return Task.FromResult(path);
    }
    public void Delete(string? path) { if (path is not null) Deleted.Add(path); }
}
sealed class TestDb(bool failSave) : ApplicationDbContext(
    new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=unused;Database=unused;Integrated Security=true").Options)
{
    public int SaveCount { get; private set; }
    // No connection is opened: this test substitutes only the commit failure boundary.
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return failSave ? throw new DbUpdateException("Simulated database failure") : Task.FromResult(1);
    }
}
