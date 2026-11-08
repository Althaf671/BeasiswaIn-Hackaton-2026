using BeasiswaIn.Web.Infrastructures.Identity;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Pages.Students;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class LandingModel : PageModel
{
    private const string AdminRedirect = "/dashboard";
    private const string StudentRedirect = "/main-page";
    private const string OnboardingRedirect = "/onboarding";

    private readonly IUserContext _userContext;

    public LandingModel(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public IActionResult OnGet()
    {
        if (!_userContext.IsAuthenticated)
            return Page();

        // Admin selalu ke dashboard, cookie onboarding tidak berlaku.
        if (_userContext.IsInRole(AppRoles.Admin))
            return Redirect(AdminRedirect);

        if (_userContext.IsInRole(AppRoles.Student))
            return Redirect(ProfilBeasiswaGate.IsDone(HttpContext) ? StudentRedirect : OnboardingRedirect);

        return Page();
    }
}