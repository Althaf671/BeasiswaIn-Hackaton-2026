using AspNetCoreHero.ToastNotification.Abstractions;
using BeasiswaIn.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages.Dashboard;

public class DashboardPageModel : PageModel
{
    protected readonly IUserContext _userContext;
    protected readonly INotyfService _toast;

    public DashboardPageModel(
        IUserContext userContext,
        INotyfService notyfService)
    {
        _userContext = userContext;
        _toast = notyfService;
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