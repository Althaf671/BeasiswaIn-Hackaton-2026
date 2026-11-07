using AspNetCoreHero.ToastNotification.Abstractions;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Infrastructures.DependencyInjection;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Scholarships;
using BeasiswaIn.Web.Models.Students;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BeasiswaIn.Web.Pages.Students.Scholarships;

[EnableRateLimiting(RateLimitRegistry.LowRateResourcePolicy)]
public class IndexModel : PageModel
{
    private readonly INotyfService _toast;
    private readonly IScholarshipService _scholarshipService;
    private readonly ILeaderboardService _leaderboardService;
    private readonly IUserContext _userContext;

    [BindProperty]
    public StudentSubmissionRequest StudentSubmissionRequest { get; set; } = new();

    public bool ShowSubmissionModal { get; private set; }
    public bool SubmissionSucceeded { get; private set; }

    public ScholarshipListResponse Scholarships { get; set; } = new();

    public IndexModel(
        IScholarshipService scholarshipService,
        INotyfService notyfService,
        ILeaderboardService leaderboardService,
        IUserContext userContext)
    {
        _scholarshipService = scholarshipService;
        _toast = notyfService;
        _leaderboardService = leaderboardService;
        _userContext = userContext;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        if (TempData["ToastSuccess"] is string message)
            _toast.ToastSuccess("Sukses", message);

        SubmissionSucceeded = TempData["SubmissionSucceeded"] is true;
        await LoadAllScholarshipAsync(cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostSubmitAsync(CancellationToken cancellationToken)
    {
        var appUserId = _userContext.UserId;
        if (appUserId is null)
            return Unauthorized();

        if (ModelState.IsValid)
        {
            var result = await _leaderboardService.SubmitAsync(
                appUserId,
                StudentSubmissionRequest,
                cancellationToken);
            if (result.IsSuccess)
            {
                if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    return new JsonResult(new { success = true });

                TempData["SubmissionSucceeded"] = true;
                return RedirectToPage();
            }

            foreach (var error in result.Errors)
            {
                var key = string.IsNullOrEmpty(error.Field)
                    ? string.Empty
                    : $"{nameof(StudentSubmissionRequest)}.{error.Field}";
                ModelState.AddModelError(key, error.Description!);
            }
        }

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            return new JsonResult(new
            {
                errors = ModelState
                    .Where(x => x.Value is { Errors.Count: > 0 })
                    .ToDictionary(x => x.Key, x => x.Value!.Errors.Select(error => error.ErrorMessage).ToArray())
            }) { StatusCode = StatusCodes.Status422UnprocessableEntity };
        }

        ShowSubmissionModal = true;
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

