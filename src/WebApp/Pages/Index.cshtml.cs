using AspNetCoreHero.ToastNotification.Abstractions;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Infrastructures.Identity;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Scholarships;
using BeasiswaIn.Web.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages;

public class IndexModel : PageModel
{
    // private const string DefaultRedirect = "/";
    // private const string AdminRedirect = "/dashboard";
    // private const string StudentRedirect = "/main-page";

    // private readonly IUserContext _userContext;

    // public IndexModel(IUserContext userContext)
    // {
    //     _userContext = userContext;
    // }

    // public async Task<IActionResult> OnGetAsync()
    // {
    //     var isAdmin = _userContext.IsInRole(AppRoles.Admin);
    //     var isStudent = _userContext.IsInRole(AppRoles.Student);

    //     if (_userContext.IsAuthenticated)
    //     {
    //         if (isAdmin)
    //             return Redirect(AdminRedirect);

    //         if (isStudent)
    //             return Redirect(StudentRedirect);
    //     }

    //     return Page();
    // }

    public string UserName => !_userContext.IsAuthenticated
        ? "Pengunjung"
        : string.IsNullOrWhiteSpace(_userContext.Username)
            ? "Pengguna"
            : string.Join(" ", _userContext.Username.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Take(2));

    private readonly IScholarshipService _scholarshipService;
    private readonly INotyfService _toast;
    private readonly IUserContext _userContext;

    public ScholarshipListResponse TrendingScholarship { get; set; } = new();
    public ScholarshipListResponse RecommendedScholarship { get; set; } = new();
    public ScholarshipListResponse LatestScholarship { get; set; } = new();

    public IndexModel(
        IScholarshipService scholarshipService,
        INotyfService notyfService,
        IUserContext userContext)
    {
        _scholarshipService = scholarshipService;
        _toast = notyfService;
        _userContext = userContext;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        if (TempData["ToastSuccess"] is string message)
            _toast.ToastSuccess("Sukses", message);

        await LoadLatestScholarshipAsync(cancellationToken);

        // Personal recommendations require an authenticated account; a missing identity is
        // reported in logs, never as a user-facing error that survives into the next page.
        if (_userContext.IsAuthenticated && !string.IsNullOrWhiteSpace(_userContext.UserId))
            await LoadRecommendedScholarshipAsync(cancellationToken);

        await LoadTrendingScholarshipAsync(cancellationToken);

        return Page();
    }

    private async Task LoadTrendingScholarshipAsync(CancellationToken cancellationToken)
    {
        var data = await _scholarshipService.GetAllTrendingAsync(1, 100, cancellationToken);
        if (data.IsFailure)
        {
            _toast.ToastError("Gagal", data.Error.Description!);
            return;
        }
        
        TrendingScholarship = data.Value!;
    }

    private async Task LoadLatestScholarshipAsync(CancellationToken cancellationToken)
    {
        var data = await _scholarshipService.GetLatestAsync(1, 100, cancellationToken);
        if (data.IsFailure)
        {
            _toast.ToastError("Gagal", data.Error.Description!);
            return;
        }

        LatestScholarship = data.Value!;
    }

    private async Task LoadRecommendedScholarshipAsync(CancellationToken cancellationToken)
    {
        var appUserId = _userContext.UserId;
        if (string.IsNullOrWhiteSpace(appUserId))
        {
            _toast.ToastError("Gagal", "Akun anda tidak ditemukan.");
            return;
        }

        var data = await _scholarshipService.GetMyRecommendedAsync(appUserId, 1, 100, cancellationToken);
        if (data.IsFailure)
        {
            _toast.ToastError("Gagal", data.Error.Description!);
            return;
        }

        RecommendedScholarship = data.Value!;
    }
}
