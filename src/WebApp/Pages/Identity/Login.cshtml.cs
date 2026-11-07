/*
 * BeasiswaIn
 *
 * Copyright (c) 2026 Althaf671
 *
 * Modification, redistribution, and commercial use are prohibited.
 *
 * See the project README for the complete license terms.
 */

using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Auth;
using BeasiswaIn.Web.Infrastructures.DependencyInjection;

namespace BeasiswaIn.Web.Pages.Identity;

[AllowAnonymous]
[EnableRateLimiting(RateLimitRegistry.HighRateAuthPolicy)]
public class LoginModel : PageModel
{
    private const string DefaultRedirect = "/dashboard";

    private readonly IIdentityService _identityService;
    private readonly INotyfService _toast;

    public LoginModel(IIdentityService identityService, INotyfService toast)
    {
        _identityService = identityService;
        _toast = toast;
    }

    [BindProperty]
    public LoginRequest Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(SafeReturnUrl(returnUrl));

        ReturnUrl = SafeReturnUrl(returnUrl);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = SafeReturnUrl(returnUrl);

        if (!ModelState.IsValid)
        {
            _toast.ToastError("Gagal Masuk", "Periksa kembali email dan kata sandi Anda.");
            return Page();
        }

        var result = await _identityService.LoginAsync(new LoginRequest
        {
            Email = Input.Email.Trim(),
            Password = Input.Password
        });

        if (result.IsFailure)
        {
            if (result.Error == AuthenticationErrors.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new
                {
                    ReturnUrl,
                    RememberMe = false
                });
            }

            _toast.ToastError("Gagal Masuk", result.Error.Description!);
            return Page();
        }

        _toast.ToastInfo("Berhasil Masuk", result.Value!);
        return LocalRedirect(ReturnUrl);
    }

    private string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : DefaultRedirect;
}