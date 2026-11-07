
using AspNetCoreHero.ToastNotification.Abstractions;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Infrastructures.DependencyInjection;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Auth;
using BeasiswaIn.Web.Models.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BeasiswaIn.Web.Pages.Dashboard.AccountSettings;

[EnableRateLimiting(RateLimitRegistry.LowRateAuthPolicy)]
public class SettingModel : PageModel
{
    private readonly IDashboardService _dashboardService;
    private readonly IIdentityService _identityService;
    private readonly INotyfService _toast;

    public GetMyAccountResponse MyAccountInfo { get; set; } = new();
    public AuditLogListResponse TableAuditLogItems { get; set; } = new();

    public SettingModel(
        IDashboardService dashboardService,
        IIdentityService identityService,
        INotyfService notyfService)
    {
        _dashboardService = dashboardService;
        _identityService = identityService;
        _toast = notyfService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (TempData["ToastSuccess"] is string message)
            _toast.ToastSuccess("Sukses", message);

        await LoadMyInfoAsync();
        await LoadAuditLogList(cancellationToken);

        return Page();
    }

    private async Task LoadMyInfoAsync()
    {
        var data = await _identityService.GetMyAccountAsync();
        if (data.IsFailure)
        {
            _toast.ToastError("Gagal", data.Error.Description!);
            return;
        }

        MyAccountInfo = data.Value!;
    }

    private async Task LoadAuditLogList(CancellationToken cancellationToken)
    {
        var data = await _dashboardService.GetAuditLogListAsync(1, 100, cancellationToken);
        if (data.IsFailure)
        {
            _toast.ToastError("Gagal", data.Error.Description!);
            return;
        }

        TableAuditLogItems = data.Value!;
    }
}