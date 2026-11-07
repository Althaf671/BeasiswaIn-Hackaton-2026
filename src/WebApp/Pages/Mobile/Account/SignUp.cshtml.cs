using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages.Mobile.Account;

[AllowAnonymous]
public class SignUpModel : PageModel
{
    public void OnGet()
    {
    }

    public IActionResult OnPost() => ProfilBeasiswaGate.AfterLogin(Request);

    public IActionResult OnPostGoogle() => ProfilBeasiswaGate.AfterLogin(Request);
}
