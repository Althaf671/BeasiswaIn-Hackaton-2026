using AspNetCoreHero.ToastNotification.Abstractions;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Infrastructures.DependencyInjection;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Scholarships;
using BeasiswaIn.Web.Pages;
using BeasiswaIn.Web.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BeasiswaIn.Web.Pages.Students.Scholarships;

[EnableRateLimiting(RateLimitRegistry.LowRateResourcePolicy)]
public class IndexModel : PageModel
{
    private readonly INotyfService _toast;
    private readonly IScholarshipService _scholarshipService;

    public ScholarshipListResponse Scholarships { get; set; } = new();

    public IndexModel(
        IScholarshipService scholarshipService,
        INotyfService notyfService)
    {
        _scholarshipService = scholarshipService;
        _toast = notyfService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        if (TempData["ToastSuccess"] is string message)
            _toast.ToastSuccess("Sukses", message);

        await LoadAllScholarshipAsync(cancellationToken);

        return Page();
    }

    private async Task LoadAllScholarshipAsync(CancellationToken cancellationToken)
    {
        var data = await _scholarshipService.GetAllAsync(1, 100, cancellationToken);
        if (data.IsFailure)
        {
            _toast.ToastError("Gagal", data.Error.Description!);
            return;
        }
        
        Scholarships = data.Value!;
    }
}

