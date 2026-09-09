using Intravision.Filters;
using Microsoft.AspNetCore.Mvc;
namespace Intravision.Controllers;

public class AppController(IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("/")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Customer() => Shell();

    [HttpGet("/admin")]
    [ServiceFilter(typeof(AdminKeyFilter), Order = -2000)]
    public IActionResult Admin() => Shell();

    private IActionResult Shell() => PhysicalFile(Path.Combine(environment.WebRootPath, "app", "index.html"), "text/html; charset=utf-8");
}
