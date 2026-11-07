using BeasiswaIn.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages;

public class HomeModel(IUserContext userContext, IIdentityService identityService) : PageModel
{
    public bool IsAuthenticated => userContext.IsAuthenticated;
    public string Username => userContext.Username ?? "Student";
    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        var result = await identityService.LogoutAsync();
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Description;
            return Page();
        }

        return RedirectToPage("/Index");
    }
}
