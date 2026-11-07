using AspNetCoreHero.ToastNotification.Abstractions;
using BeasiswaIn.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages.Students;

public class StudentProfileModel : PageModel
{
    protected readonly IUserContext _userContext;
    protected readonly INotyfService _toast;
    protected readonly IStudentProfileService _studentProfileService;

    public StudentProfileModel(
        IUserContext userContext,
        INotyfService notyfService,
        IStudentProfileService studentProfileService)
    {
        _userContext = userContext;
        _toast = notyfService;
        _studentProfileService = studentProfileService;
    }

    protected void KeepOnly(string prefix)
    {
        var prefixDot = prefix + ".";

        foreach (var key in ModelState.Keys
            .Where(k => k.Length > 0 && !k.StartsWith(prefixDot))
            .ToList())
        {
            ModelState.Remove(key);
        }
    }

    protected bool IsAjax =>
        Request.Headers.XRequestedWith == "XMLHttpRequest";

    protected JsonResult AjaxErrors()
    {
        return new(new
        {
            errors = ModelState
                .Where(e => e.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray())
        })

        { StatusCode = StatusCodes.Status422UnprocessableEntity };
    }    
}