using BeasiswaIn.Web.Infrastructures.Identity;
using BeasiswaIn.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages;

public class IndexModel : PageModel
{
    private const string DefaultRedirect = "/";
    private const string AdminRedirect = "/dashboard";
    private const string StudentRedirect = "/main-page";

    private readonly IUserContext _userContext;

    public IndexModel(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var isAdmin = _userContext.IsInRole(AppRoles.Admin);
        var isStudent = _userContext.IsInRole(AppRoles.Student);

        if (_userContext.IsAuthenticated)
        {
            if (isAdmin)
                return Redirect(AdminRedirect);

            if (isStudent)
                return Redirect(StudentRedirect);
        }

        return Page();
    }
}
