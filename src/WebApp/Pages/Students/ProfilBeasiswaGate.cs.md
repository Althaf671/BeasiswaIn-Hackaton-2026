using System.Security.Claims;

namespace BeasiswaIn.Web.Pages.Students;

public static class ProfilBeasiswaGate
{
    private const string CookiePrefix = "profilBeasiswaDone_";

    public static bool IsDone(HttpContext context)
    {
        var name = CookieName(context);
        return name is not null && context.Request.Cookies.ContainsKey(name);
    }

    public static void MarkDone(HttpContext context)
    {
        var name = CookieName(context);
        if (name is null) return;

        context.Response.Cookies.Append(name, "1", new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });
    }

    // Cookie per user, supaya akun lain di browser yang sama tidak ikut dianggap selesai.
    private static string? CookieName(HttpContext context)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(id) ? null : CookiePrefix + id;
    }
}