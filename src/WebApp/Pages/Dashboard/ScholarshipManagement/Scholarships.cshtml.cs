using AspNetCoreHero.ToastNotification.Abstractions;
using BeasiswaIn.Web.Common.Enums;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Scholarships;
using BeasiswaIn.Web.Pages.Dashboard.Shared;
using Microsoft.AspNetCore.Mvc;

namespace BeasiswaIn.Web.Pages.Dashboard.ScholarshipManagement;

public class ScholarshipModel : ScholarshipPageModel
{
    [BindProperty]
    public CreateScholarshipRequest CreateScholarshipRequest { get; set; } = new();

    [BindProperty]
    public CreateScholarshipCategoryRequest CreateScholarshipCategoryRequest { get; set; } = new();

    public IReadOnlyList<MetricViewModel> MetricItem { get; set; } = [];
    public ScholarshipListResponse TableScholarshipItems { get; set; } = new();
    public ScholarshipCategoryListResponse TableScholarshipCategoryItems { get; set; } = new();
    public IReadOnlyList<ScholarshipCategoryOptions> ScholarshipCategoryOptions { get; set; } = [];

    public ScholarshipModel(
        IScholarshipService scholarship, 
        IScholarshipCategoryService scholarshipCategory, 
        IUserScholarshipPreferenceService userScholarshipPreference, 
        IUserContext userContext, 
        INotyfService notifyService) : base(scholarship, scholarshipCategory, userScholarshipPreference, userContext, notifyService)
    {
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        if (TempData["ToastSuccess"] is string message)
            _toast.ToastSuccess("Sukses", message);
            
        await LoadScholarshipDataTableAsync(cancellationToken);
        await LoadScholarshipCategoryDataTableAsync(cancellationToken);
        await LoadMetricDataAsync(cancellationToken);
        await LoadCategoryOptionsAsync(cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostCreateScholarshipAsync(
        CancellationToken cancellationToken)
    {
        KeepOnly(nameof(CreateScholarshipRequest));

        if (!ModelState.IsValid)
        {
            if (IsAjax) 
                return AjaxErrors();

            await LoadScholarshipDataTableAsync(cancellationToken);
            await LoadScholarshipCategoryDataTableAsync(cancellationToken);
            await LoadMetricDataAsync(cancellationToken);
            await LoadCategoryOptionsAsync(cancellationToken);

            return Page();
        }

        var res = await _scholarshipService.CreateAsync(CreateScholarshipRequest, cancellationToken);
        if (res.IsFailure)
        {
            foreach (var error in res.Errors)
            {
                var key = string.IsNullOrEmpty(error.Field)
                    ? string.Empty
                    : $"{nameof(CreateScholarshipRequest)}.{error.Field}";

                ModelState.AddModelError(key, error.Description!);
            }

            if (IsAjax)
                return AjaxErrors();

            await LoadMetricDataAsync(cancellationToken);
            await LoadScholarshipDataTableAsync(cancellationToken);
            await LoadScholarshipCategoryDataTableAsync(cancellationToken);
            await LoadCategoryOptionsAsync(cancellationToken);
            
            return Page();
        }

        TempData["ToastSuccess"] = "Beasiswa berhasil ditambahkan.";

        if (IsAjax) 
            return new JsonResult(new { redirect = Request.Path.Value });

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateScholarshipCategoryAsync(
        CancellationToken cancellationToken)
    {
        KeepOnly(nameof(CreateScholarshipCategoryRequest));

        if (!ModelState.IsValid)
        {
            if (IsAjax) 
                return AjaxErrors();

            await LoadScholarshipDataTableAsync(cancellationToken);
            await LoadScholarshipCategoryDataTableAsync(cancellationToken);
            await LoadMetricDataAsync(cancellationToken);
            await LoadCategoryOptionsAsync(cancellationToken);

            return Page();
        }

        var res = await _scholarshipCategoryService.CreateAsync(
            CreateScholarshipCategoryRequest, 
            cancellationToken);
        if (res.IsFailure)
        {
            foreach (var error in res.Errors)
            {
                var key = string.IsNullOrEmpty(error.Field)
                    ? string.Empty
                    : $"{nameof(CreateScholarshipCategoryRequest)}.{error.Field}";

                ModelState.AddModelError(key, error.Description!);
            }

            if (IsAjax)
                return AjaxErrors();

            await LoadMetricDataAsync(cancellationToken);
            await LoadScholarshipCategoryDataTableAsync(cancellationToken);
            await LoadScholarshipDataTableAsync(cancellationToken);
            await LoadCategoryOptionsAsync(cancellationToken);

            return Page();
        }

        TempData["ToastSuccess"] = "Lomba berhasil ditambahkan.";

        if (IsAjax) 
            return new JsonResult(new { redirect = Request.Path.Value });

        return RedirectToPage();
    }

    private async Task LoadMetricDataAsync(CancellationToken cancellationToken)
    {
        var data = await _scholarshipService.GetMetricAsync(cancellationToken);
        if (data.IsFailure)
        {
            _toast.ToastError("Gagal", $"Gagal memuat metric: {data.Error.Description}");
        }
        else
        {
            MetricItem =
            [
                new()
                {
                    Title = "Total Beasiswa",
                    Description = "Jumlah seluruh beasiswa yang terdaftar.",
                    Icon = "graduation-cap",
                    Value = data.Value!.TotalScholarship.Value.ToString(),
                    Trend = MetricTrend.Nutral,
                    TrendValue = "0"
                },
                new()
                {
                    Title = "Beasiswa Aktif",
                    Description = "Jumlah beasiswa yang memenuhi kriteria kelayakan.",
                    Icon = "badge-check",
                    Value = data.Value!.TotalEligibleScholarship.Value.ToString(),
                    Trend = MetricTrend.Positive,
                    TrendValue = "0"
                },
                new()
                {
                    Title = "Pendaftaran Dibuka",
                    Description = "Jumlah beasiswa yang sedang menerima pendaftaran.",
                    Icon = "calendar-check",
                    Value = data.Value!.TotalScholarshipRegistrationOpened.Value.ToString(),
                    Trend = MetricTrend.Positive,
                    TrendValue = "0"
                },
                new()
                {
                    Title = "Belum Dibuka",
                    Description = "Jumlah beasiswa dengan periode pendaftaran yang belum dimulai.",
                    Icon = "calendar-clock",
                    Value = data.Value!.TotalScholarshipNotOpenedMetric.Value.ToString(),
                    Trend = MetricTrend.Nutral,
                    TrendValue = "0"
                }
            ];       
        }
    }

    private async Task LoadScholarshipDataTableAsync(CancellationToken cancellationToken)
    {
        var dataTable = await _scholarshipService.GetAllAsync(1, 100, cancellationToken);
        if (dataTable.IsFailure)
        {
            _toast.ToastError("Gagal", dataTable.Error.Description!);
            return;
        }

        TableScholarshipItems = dataTable.Value!;
    } 

    private async Task LoadScholarshipCategoryDataTableAsync(CancellationToken cancellationToken)
    {
        var dataTable = await _scholarshipCategoryService.GetAllAsync(1, 100, cancellationToken);
        if (dataTable.IsFailure)
        {
            _toast.ToastError("Gagal", dataTable.Error.Description!);
            return;
        }

        TableScholarshipCategoryItems = dataTable.Value!;
    }

    private async Task LoadCategoryOptionsAsync(CancellationToken cancellationToken)
    {
        var data = await _scholarshipCategoryService.GetOptionsAsync(cancellationToken);
        if (data.IsFailure)
        {
            _toast.ToastError("Gagal", data.Error.Description!);
            return;
        }

        ScholarshipCategoryOptions = data.Value!;
    }
}