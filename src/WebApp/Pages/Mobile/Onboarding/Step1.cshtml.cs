using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages.Mobile.Onboarding;

public class Step1Model : PageModel
{
    public IReadOnlyList<string> SuggestedCategories { get; } =
    [
        "Fully Funded",
        "Ptn",
        "Swasta",
        "Luar Negeri",
        "Dalam Negeri"
    ];

    public IActionResult OnGet()
    {
        if (ProfilBeasiswaGate.IsDone(Request))
            return Redirect("/");

        return Page();
    }
}
