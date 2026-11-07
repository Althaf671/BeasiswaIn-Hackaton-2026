using Microsoft.AspNetCore.Mvc;

namespace BeasiswaIn.Web.Pages.Mobile;

public static class ProfilBeasiswaGate
{
    public const string CookieName = "profilBeasiswaDone";

    public static bool IsDone(HttpRequest request) =>
        request.Cookies.ContainsKey(CookieName);

    public static void MarkDone(HttpResponse response)
    {
        response.Cookies.Append(CookieName, "1", new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = false,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });
    }

    public static IActionResult AfterLogin(HttpRequest request) =>
        IsDone(request)
            ? new RedirectResult("/")
            : new RedirectResult("/profil-beasiswa");
}
