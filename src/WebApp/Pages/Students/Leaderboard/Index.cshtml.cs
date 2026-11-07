using AspNetCoreHero.ToastNotification.Abstractions;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Infrastructures.DependencyInjection;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Students;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BeasiswaIn.Web.Pages.Students.Leaderboard;

[EnableRateLimiting(RateLimitRegistry.LowRateResourcePolicy)]
public class IndexModel : PageModel
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly IUserContext _userContext;
    private readonly INotyfService _toast;

    public LeaderboardResponse Leaderboard { get; private set; } = new();
    public IReadOnlyList<LeaderboardStudentResponse> Rows => Leaderboard.Students.Skip(3).ToList();

    public IndexModel(
        ILeaderboardService leaderboardService,
        IUserContext userContext,
        INotyfService notyfService)
    {
        _leaderboardService = leaderboardService;
        _userContext = userContext;
        _toast = notyfService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var appUserId = _userContext.UserId;
        if (appUserId is null)
            return Unauthorized();

        var result = await _leaderboardService.GetAsync(appUserId, cancellationToken);
        if (result.IsFailure)
            _toast.ToastError("Gagal", result.Error.Description!);
        else
            Leaderboard = result.Value!;

        return Page();
    }
}
