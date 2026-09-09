using System.Security.Cryptography;
using Intravision.Data;
using Intravision.Models.Entities;
using Microsoft.AspNetCore.DataProtection;

namespace Intravision.Services;

public sealed class CustomerWalletAccessor(ApplicationDbContext db, IDataProtectionProvider protection)
{
    private const string CookieName = "Intravision.Wallet";
    private readonly IDataProtector protector = protection.CreateProtector("Intravision.CustomerWallet.v1");

    public async Task<CustomerWallet?> GetAsync(HttpContext context, bool create, CancellationToken cancellationToken)
    {
        if (context.Request.Cookies.TryGetValue(CookieName, out var cookie))
        {
            try
            {
                if (Guid.TryParse(protector.Unprotect(cookie), out var id))
                {
                    var existing = await db.CustomerWallets.FindAsync([id], cancellationToken);
                    if (existing is not null) return existing;
                }
            }
            catch (CryptographicException) { /* An invalid cookie never grants another wallet's balance. */ }
        }
        if (!create) return null;
        var wallet = new CustomerWallet { Id = Guid.NewGuid() };
        db.CustomerWallets.Add(wallet);
        await db.SaveChangesAsync(cancellationToken);
        context.Response.Cookies.Append(CookieName, protector.Protect(wallet.Id.ToString()), new CookieOptions
        {
            HttpOnly = true, Secure = context.Request.IsHttps, SameSite = SameSiteMode.Strict,
            IsEssential = true, MaxAge = TimeSpan.FromDays(30), Path = "/"
        });
        return wallet;
    }
}