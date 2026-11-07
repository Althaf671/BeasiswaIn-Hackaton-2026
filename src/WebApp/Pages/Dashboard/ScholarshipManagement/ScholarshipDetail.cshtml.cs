using AspNetCoreHero.ToastNotification.Abstractions;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Scholarships;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages.Dashboard.ScholarshipManagement;

public class ScholarshipDetailModel : ScholarshipPageModel
{
    [BindProperty]
    public UpdateScholarshipRequest UpdateScholarshipRequest { get; set; } = new();

    public ScholarshipResponse ScholarshipInfo { get; set; } = new();
    public IReadOnlyList<ScholarshipCategoryOptions> ScholarshipCategoryOptions { get; set; } = [];

    public ScholarshipDetailModel(
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
            
        await LoadScholarshipInfoAsync(id, cancellationToken);
        await LoadCategoryOptionsAsync(cancellationToken);


        UpdateScholarshipRequest = new UpdateScholarshipRequest
        {
            Name = ScholarshipInfo.Name,
            Description = ScholarshipInfo.Description,
            TotalPrize = ScholarshipInfo.TotalPrize,
            GuideUrl = ScholarshipInfo.GuideUrl,
            RegistrationUrl = ScholarshipInfo.RegistrationUrl,
            Requirement = ScholarshipInfo.Requirement,
            ContactPerson = ScholarshipInfo.ContactPerson,
            IsEligible = ScholarshipInfo.IsEligible,
            RegistrationStartAt = ScholarshipInfo.RegistrationStartAt,
            RegistrationEndAt = ScholarshipInfo.RegistrationEndAt,
            CategoryIds = ScholarshipInfo.CategoryIds
        };

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateScholarshipAsync(
        string id,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjax) 
                return AjaxErrors();

            await LoadScholarshipInfoAsync(id, cancellationToken);
            await LoadCategoryOptionsAsync(cancellationToken);

            return Page();
        }

        var res = await _scholarshipService.UpdateAsync(
            id, 
            UpdateScholarshipRequest, 
            cancellationToken);
        if (res.IsFailure)
        {
            foreach (var error in res.Errors)
            {
                var key = string.IsNullOrEmpty(error.Field)
                    ? string.Empty
                    : $"{nameof(UpdateScholarshipRequest)}.{error.Field}";

                ModelState.AddModelError(key, error.Description!);
            }

            if (IsAjax)
                return AjaxErrors();

            await LoadScholarshipInfoAsync(id, cancellationToken);
            await LoadCategoryOptionsAsync(cancellationToken);

            return Page();
        }

        TempData["ToastSuccess"] = "Beasiswa berhasil diedit.";

        if (IsAjax) 
            return new JsonResult(new { redirect = Request.Path.Value });

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        string id, 
        CancellationToken cancellationToken)
    {
        var res = await _scholarshipService.DeleteAsync(id, cancellationToken);
        if (res.IsFailure)
        {
            await LoadScholarshipInfoAsync(id, cancellationToken);
            await LoadCategoryOptionsAsync(cancellationToken);

            _toast.ToastError("Gagal", res.Error.Description!);
            return Redirect($"/dashboard/scholarship-management/scholarship/{Uri.EscapeDataString(id)}");
        }

        TempData["SuccessMessage"] = "Beasiswa berhasil dihapus.";

        if (IsAjax) 
            return new JsonResult(new { redirect = Request.Path.Value });
            
        return Redirect("/dashboard/scholarship-management");
    }

    private async Task LoadScholarshipInfoAsync(
        string id, 
        CancellationToken cancellationToken)
    {
        var data = await _scholarshipService.GetByIdAsync(id, cancellationToken);
        if (data.IsFailure)
        {
            _toast.ToastError("Gagal", data.Error.Description!);
            return;
        }

        ScholarshipInfo = data.Value!;
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