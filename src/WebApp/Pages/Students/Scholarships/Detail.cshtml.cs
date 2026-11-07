using AspNetCoreHero.ToastNotification.Abstractions;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Infrastructures.DependencyInjection;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Scholarships;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BeasiswaIn.Web.Pages.Students.Scholarships;

[EnableRateLimiting(RateLimitRegistry.LowRateResourcePolicy)]
public class DetailModel : PageModel
{
    private readonly IScholarshipService _scholarshipService;
    private readonly INotyfService _toast;

    public ScholarshipResponse ScholarshipInfo { get; set; } = new();

    public DetailModel(
        IScholarshipService scholarshipService,
        INotyfService notyfService)
    {
        _scholarshipService = scholarshipService;
        _toast = notyfService;
    }

    public async Task<IActionResult> OnGetAsync(
        [FromRoute] string? id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        if (TempData["ToastSuccess"] is string message)
            _toast.ToastSuccess("Sukses", message);

        return await LoadScholarshipInfoAsync(id, cancellationToken);
    }

    private async Task<IActionResult> LoadScholarshipInfoAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var data = await _scholarshipService.GetByIdAsync(id, cancellationToken);
        if (data.IsFailure)
        {
            return NotFound(data.Error.Description ?? "Beasiswa tidak ditemukan.");
        }

        ScholarshipInfo = data.Value!;
        return Page();
    }
}
