using Intravision.Data;
using Intravision.Filters;
using Intravision.Services;
using Intravision.Services.Contracts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<AdminKeyFilter>();
builder.Services.AddScoped<IDrinkImageStore, DrinkImageStore>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICatalogQueries, CatalogQueries>();
builder.Services.AddSingleton<IChangeCalculator, ChangeCalculator>();
builder.Services.AddSingleton<IDrinkImportReader, JsonDrinkImportReader>();
builder.Services.AddScoped<IDrinkImportService, DrinkImportService>();
builder.Services.AddScoped<CustomerWalletAccessor>();
builder.Services.AddScoped<IVendingService, VendingService>();
builder.Services.AddControllers();
builder.Services.AddScoped<ApiAntiforgeryFilter>();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context => {
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new { error = "Не удалось выполнить запрос. Проверьте подключение к базе данных и повторите попытку." });
}));
if (!app.Environment.IsDevelopment()) app.UseHsts();

app.UseRequestLocalization(new RequestLocalizationOptions().SetDefaultCulture("en-US").AddSupportedCultures("en-US").AddSupportedUICultures("en-US"));
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllers().WithStaticAssets();
app.Run();