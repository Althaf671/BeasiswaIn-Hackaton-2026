using BeasiswaIn.Web.Interfaces;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages.Identity;

[AllowAnonymous]
public class LoginStudentModel(IIdentityService identityService) : PageModel
{
    public string? ErrorMessage { get; private set; }
    public string ReturnUrl { get; private set; } = "/main-page";

    public IActionResult OnGet(string? returnUrl = null, bool googleError = false)
    {
        ReturnUrl = SafeReturnUrl(returnUrl);
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        if (googleError)
            ErrorMessage = "Login Google dibatalkan atau gagal. Silakan coba lagi.";
        return Page();
    }

    public IActionResult OnPostLogin(string? returnUrl = null)
    {
        ReturnUrl = SafeReturnUrl(returnUrl);
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        var callback = Url.Page("/Identity/LoginStudent", "Callback", new { returnUrl = ReturnUrl })!;
        var result = identityService.CreateGoogleLoginProperties(callback);
        if (result.IsFailure)
            return Failure(result.Error.Description!);

        return Challenge(result.Value!, GoogleDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        ReturnUrl = SafeReturnUrl(returnUrl);
        var result = await identityService.LoginWithGoogleAsync(remoteError);
        if (result.IsFailure)
            return Failure(result.Error.Description!);

        return LocalRedirect(ReturnUrl);
    }

    private string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : "/main-page";

    private IActionResult Failure(string message)
    {
        ErrorMessage = message;
        return Page();
    }
}
