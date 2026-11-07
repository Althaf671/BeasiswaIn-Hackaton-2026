using BeasiswaIn.Web.Infrastructures.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages.Identity;

public class AccessDeniedModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.IsInRole(AppRoles.Admin))
            return LocalRedirect("/dashboard");

        if (User.IsInRole(AppRoles.Student))
            return LocalRedirect("/main-page");

        return StatusCode(StatusCodes.Status403Forbidden);
    }
}