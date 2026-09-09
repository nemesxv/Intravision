using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Intravision.Filters;

public sealed class AdminKeyFilter(IConfiguration configuration) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        context.HttpContext.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        var expected = configuration["Admin:SecretKey"];
        var supplied = context.HttpContext.Request.Query["key"];
        if (string.IsNullOrWhiteSpace(expected) || supplied.Count != 1 ||
            !CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
                SHA256.HashData(Encoding.UTF8.GetBytes(supplied[0] ?? ""))))
        {
            context.Result = new ObjectResult(new { error = "Доступ запрещён. Проверьте секретный ключ в адресе страницы." }) { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}