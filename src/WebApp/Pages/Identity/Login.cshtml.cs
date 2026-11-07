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
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Auth;
using BeasiswaIn.Web.Infrastructures.DependencyInjection;
using BeasiswaIn.Web.Infrastructures.Identity;

namespace BeasiswaIn.Web.Pages.Identity;

[AllowAnonymous]
[EnableRateLimiting(RateLimitRegistry.HighRateAuthPolicy)]
public class LoginModel : PageModel
{
    private const string AdminRedirect = "/dashboard";
    private const string StudentRedirect = "/main-page";

    private readonly IIdentityService _identityService;
    private readonly INotyfService _toast;

    public LoginModel(IIdentityService identityService, INotyfService toast)
    {
        _identityService = identityService;
        _toast = toast;
    }

    public string ReturnUrl { get; private set; } = StudentRedirect;

    // nama field di Login.cshtml: name="username" dan name="password"
    [BindProperty(Name = "username")]
    public string Username { get; set; } = string.Empty;

    [BindProperty(Name = "password")]
    public string Password { get; set; } = string.Empty;

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectAuthenticated(returnUrl);

        ReturnUrl = ResolveStudentUrl(returnUrl);

        if (TempData["ToastError"] is string message)
            _toast.ToastError("Gagal Masuk", message);

        return Page();
    }

    // POST /identity/login (email + password) = KHUSUS ADMIN -> /dashboard
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = ResolveAdminUrl(returnUrl);

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            _toast.ToastError("Gagal Masuk", "Periksa kembali email dan kata sandi Anda.");
            return Page();
        }

        var result = await _identityService.LoginAsync(new LoginRequest
        {
            Email = Username.Trim(),
            Password = Password
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

    // POST /identity/login?handler=Google = KHUSUS STUDENT, tidak pernah ke admin
    public IActionResult OnPostGoogle(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectAuthenticated(returnUrl);

        ReturnUrl = ResolveStudentUrl(returnUrl);

        var callback = Url.Page("/Identity/Login", "Callback", new { returnUrl = ReturnUrl })!;
        var result = _identityService.CreateGoogleLoginProperties(callback);
        if (result.IsFailure)
        {
            _toast.ToastError("Gagal Masuk", result.Error.Description!);
            return Page();
        }

        return Challenge(result.Value!, GoogleDefaults.AuthenticationScheme);
    }

    // GET /identity/login?handler=Callback (balik dari Google)
    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        ReturnUrl = ResolveStudentUrl(returnUrl);

        var result = await _identityService.LoginWithGoogleAsync(remoteError);
        if (result.IsFailure)
        {
            // redirect ke URL bersih supaya form di halaman tidak ikut membawa ?handler=Callback
            TempData["ToastError"] = result.Error.Description;
            return RedirectToPage(new { returnUrl = ReturnUrl });
        }

        return LocalRedirect(ReturnUrl);
    }

    // user yang sudah punya cookie: admin ke area admin, selain itu ke area student
    private IActionResult RedirectAuthenticated(string? returnUrl) =>
        LocalRedirect(User.IsInRole(AppRoles.Admin)
            ? ResolveAdminUrl(returnUrl)
            : ResolveStudentUrl(returnUrl));

    // returnUrl admin hanya boleh di bawah /dashboard, selain itu /dashboard
    private string ResolveAdminUrl(string? returnUrl)
    {
        var url = LocalUrlOrNull(returnUrl);
        return url is not null && IsAdminPath(url) ? url : AdminRedirect;
    }

    // returnUrl student tidak boleh menunjuk /dashboard, selain itu /main-page
    private string ResolveStudentUrl(string? returnUrl)
    {
        var url = LocalUrlOrNull(returnUrl);
        return url is not null && !IsAdminPath(url) ? url : StudentRedirect;
    }

    private string? LocalUrlOrNull(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;

    private static bool IsAdminPath(string url)
    {
        var path = url.Split('?', '#')[0];

        return path.Equals(AdminRedirect, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(AdminRedirect + "/", StringComparison.OrdinalIgnoreCase);
    }
}
