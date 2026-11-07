/*
 * BeasiswaIn
 *
 * Copyright (c) 2026 Althaf671
 *
 * Modification, redistribution, and commercial use are prohibited.
 *
 * See the project README for the complete license terms.
 */

using BeasiswaIn.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages.Dashboard;

public class LogoutModel : PageModel
{
    private readonly IIdentityService _identityService;

    public LogoutModel(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public IActionResult OnGet() => Redirect("/");

    public async Task<IActionResult> OnPostAsync()
    {
        await _identityService.LogoutAsync();
        return Redirect("/");
    }
}