using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages.Students.Onboarding;

public class Step2Model : PageModel
{
    public IActionResult OnGet()
    {
        if (ProfilBeasiswaGate.IsDone(Request))
            return Redirect("/");

        return Page();
    }

    public IActionResult OnPost()
    {
        ProfilBeasiswaGate.MarkDone(Response);
        return Redirect("/");
    }
}
