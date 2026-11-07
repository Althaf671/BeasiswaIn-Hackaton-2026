using AspNetCoreHero.ToastNotification.Abstractions;
using BeasiswaIn.Web.Common.Enums;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Infrastructures.DependencyInjection;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Pages.Dashboard.Shared;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BeasiswaIn.Web.Pages.Dashboard;

[EnableRateLimiting(RateLimitRegistry.LowRateResourcePolicy)]
public class IndexModel : PageModel
{
    private readonly IUserContext _userContext;
    private readonly IDashboardService _dashboardService;
    private readonly INotyfService _toast;

    public IReadOnlyList<MetricViewModel> MetricItem { get; set; } = [];

    public IndexModel(
        IUserContext userContext,
        IDashboardService dashboardService,
        INotyfService toast)
    {
        _userContext = userContext;
        _dashboardService = dashboardService;
        _toast = toast;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
    {
        var data = await _dashboardService.GetDashboardDataAsync(cancellationToken);
        if (data.IsFailure)
        {
            _toast.ToastError("Gagal", $"Gagal memuat metric: {data.Error.Description}");
        }
        else
        {
            MetricItem =
            [
                // ==========================================
                // Registration Deadline
                // ==========================================
                new()
                {
                    Title = "Kompetisi Segera Ditutup",
                    Description = "Jumlah kompetisi yang pendaftarannya akan berakhir dalam 7 hari ke depan.",
                    Icon = "calendar-clock",
                    Value = data.Value!.TotalCompetitionClosingSoon.Value.ToString(),
                    Trend = MetricTrend.Nutral,
                    TrendValue = "0"
                },
                new()
                {
                    Title = "Beasiswa Segera Ditutup",
                    Description = "Jumlah beasiswa yang pendaftarannya akan berakhir dalam 7 hari ke depan.",
                    Icon = "calendar-clock",
                    Value = data.Value!.TotalScholarshipClosingSoon.Value.ToString(),
                    Trend = MetricTrend.Nutral,
                    TrendValue = "0"
                },
                new()
                {
                    Title = "Kompetisi Kedaluwarsa",
                    Description = "Jumlah kompetisi yang telah melewati batas waktu pendaftaran.",
                    Icon = "calendar-x",
                    Value = data.Value!.TotalExpiredCompetition.Value.ToString(),
                    Trend = MetricTrend.Nutral,
                    TrendValue = "0"
                },
                new()
                {
                    Title = "Beasiswa Kedaluwarsa",
                    Description = "Jumlah beasiswa yang telah melewati batas waktu pendaftaran.",
                    Icon = "calendar-x",
                    Value = data.Value!.TotalExpiredScholarship.Value.ToString(),
                    Trend = MetricTrend.Nutral,
                    TrendValue = "0"
                },

                // ==========================================
                // Content Activity
                // ==========================================
                new()
                {
                    Title = "Kompetisi Baru Bulan Ini",
                    Description = "Jumlah kompetisi yang ditambahkan pada bulan berjalan.",
                    Icon = "trophy",
                    Value = data.Value!.TotalCompetitionCreatedThisMonth.Value.ToString(),
                    Trend = MetricTrend.Positive,
                    TrendValue = "0"
                },
                new()
                {
                    Title = "Beasiswa Baru Bulan Ini",
                    Description = "Jumlah beasiswa yang ditambahkan pada bulan berjalan.",
                    Icon = "graduation-cap",
                    Value = data.Value!.TotalScholarshipCreatedThisMonth.Value.ToString(),
                    Trend = MetricTrend.Positive,
                    TrendValue = "0"
                },

                // ==========================================
                // Categories
                // ==========================================
                new()
                {
                    Title = "Kategori Kompetisi",
                    Description = "Jumlah kategori kompetisi yang tersedia dan aktif.",
                    Icon = "tags",
                    Value = data.Value!.TotalCompetitionCategory.Value.ToString(),
                    Trend = MetricTrend.Nutral,
                    TrendValue = data.Value!.TotalCompetitionCategory.TrendValue.ToString()
                },
                new()
                {
                    Title = "Kategori Beasiswa",
                    Description = "Jumlah kategori beasiswa yang tersedia dan aktif.",
                    Icon = "folder",
                    Value = data.Value!.TotalScholarshipCategory.Value.ToString(),
                    Trend = MetricTrend.Nutral,
                    TrendValue = data.Value!.TotalScholarshipCategory.TrendValue.ToString()
                }
            ];  
        }
    }
}

