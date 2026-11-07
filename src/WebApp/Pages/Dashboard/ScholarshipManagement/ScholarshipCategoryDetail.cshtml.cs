using AspNetCoreHero.ToastNotification.Abstractions;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Competitions;
using BeasiswaIn.Web.Models.Scholarships;
using Microsoft.AspNetCore.Mvc;

namespace BeasiswaIn.Web.Pages.Dashboard.ScholarshipManagement;

public class ScholarshipCategoryDetailModel : ScholarshipPageModel
{
    [BindProperty]
    public UpdateScholarshipCategoryRequest UpdateScholarshipCategoryRequest { get; set; } = new();
    
    public ScholarshipCategoryResponse ScholarshipCategoryInfo { get; set; } = new();
    public ScholarshipListResponse TableScholarshipItems { get; set; } = new();

    public ScholarshipCategoryDetailModel(
        IScholarshipService scholarship, 
        IScholarshipCategoryService scholarshipCategory, 
        IUserScholarshipPreferenceService userScholarshipPreference, 
        IUserContext userContext, 
        INotyfService notifyService) : base(scholarship, scholarshipCategory, userScholarshipPreference, userContext, notifyService)
    {
    }

    public async Task<IActionResult> OnGetAsync(
        string id, 
        CancellationToken cancellationToken)
    {
        if (TempData["ToastSuccess"] is string message)
            _toast.ToastSuccess("Sukses", message);
            
        await LoadScholarshipCategoryInfoAsync(id, cancellationToken);
        await LoadScholarshipDataTableAsync(id, cancellationToken);
        
        UpdateScholarshipCategoryRequest = new UpdateScholarshipCategoryRequest
        {
            Name = ScholarshipCategoryInfo.Name,
            IsActive = ScholarshipCategoryInfo.IsActive
        };

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateScholarshipCategoryAsync(
        string id,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjax) 
                return AjaxErrors();

            await LoadScholarshipCategoryInfoAsync(id, cancellationToken);
            await LoadScholarshipDataTableAsync(id, cancellationToken);

            return Page();
        }

        var res = await _scholarshipCategoryService.UpdateAsync(
            id, 
            UpdateScholarshipCategoryRequest, 
            cancellationToken);
        if (res.IsFailure)
        {
            foreach (var error in res.Errors)
            {
                var key = string.IsNullOrEmpty(error.Field)
                    ? string.Empty
                    : $"{nameof(UpdateScholarshipCategoryRequest)}.{error.Field}";

                ModelState.AddModelError(key, error.Description!);
            }

            if (IsAjax)
                return AjaxErrors();

            await LoadScholarshipCategoryInfoAsync(id, cancellationToken);
            await LoadScholarshipDataTableAsync(id, cancellationToken);

            return Page();
        }

        TempData["ToastSuccess"] = "Kategori berhasil diedit.";

        if (IsAjax) 
            return new JsonResult(new { redirect = Request.Path.Value });

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteCategoryAsync(
        string id, 
        CancellationToken cancellationToken)
    {
        var res = await _scholarshipCategoryService.DeleteAsync(id, cancellationToken);
        if (res.IsFailure)
        {
            await LoadScholarshipCategoryInfoAsync(id, cancellationToken);
            await LoadScholarshipDataTableAsync(id, cancellationToken);

            _toast.ToastError("Gagal", res.Error.Description!);
            return Redirect($"/dashboard/scholarship-management/category/{Uri.EscapeDataString(id)}");
        }

        TempData["SuccessMessage"] = "Kategori berhasil dihapus.";

        if (IsAjax) 
            return new JsonResult(new { redirect = Request.Path.Value });
            
        return Redirect("/dashboard/scholarship-management");
    }

    private async Task LoadScholarshipCategoryInfoAsync(
        string id, 
        CancellationToken cancellationToken)
    {
        var data = await _scholarshipCategoryService.GetByIdAsync(id, cancellationToken);
        if (data.IsFailure)
        {
            _toast.ToastError("Gagal", data.Error.Description!);
            return;
        }

        ScholarshipCategoryInfo = data.Value!;
    }

    private async Task LoadScholarshipDataTableAsync(
        string id, 
        CancellationToken cancellationToken)
    {
        var dataTable = await _scholarshipService.GetAllByCategoryId(id, 1, 100, cancellationToken);
        if (dataTable.IsFailure)
        {
            _toast.ToastError("Gagal", dataTable.Error.Description!);
            return;
        }

        TableScholarshipItems = dataTable.Value!;
    }
}