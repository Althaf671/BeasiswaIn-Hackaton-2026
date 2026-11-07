using AspNetCoreHero.ToastNotification.Abstractions;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Infrastructures.DependencyInjection;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Students;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BeasiswaIn.Web.Pages.Students.Account;

[EnableRateLimiting(RateLimitRegistry.LowRateResourcePolicy)]
public class ProfileModel : StudentProfileModel
{
    public StudentProfileResponse StudentProfileDetailInfo { get; private set; } = new();

    [BindProperty]
    public UpdateStudentProfileRequest UpdateStudentProfileRequest { get; set; } = new();

    public ProfileModel(
        IUserContext userContext, 
        INotyfService notyfService, 
        IStudentProfileService studentProfileService) : base(userContext, notyfService, studentProfileService)
    {
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        if (TempData["ToastSuccess"] is string message)
            _toast.ToastSuccess("Sukses", message);

        await LoadStudentInfoAsync(cancellationToken);

        UpdateStudentProfileRequest = new UpdateStudentProfileRequest
        {
            FullName = StudentProfileDetailInfo.FullName,
            Institution = StudentProfileDetailInfo.Institution,
            FinalScore = StudentProfileDetailInfo.FinalScore,
            Description = StudentProfileDetailInfo.Description
        };

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjax)
                return AjaxErrors();

            await LoadStudentInfoAsync(cancellationToken);
            return Page();
        }

        var appUserId = _userContext.UserId;
        if (appUserId is null)
        {
            _toast.ToastError("Gagal", "Akun ini tidak ditemukan");
            return Page();
        }

        var res = await _studentProfileService.UpdateAsync(
            appUserId,
            UpdateStudentProfileRequest,
            cancellationToken);
        if (res.IsFailure)
        {
            foreach (var error in res.Errors)
            {
                var key = string.IsNullOrEmpty(error.Field)
                    ? string.Empty
                    : $"{nameof(UpdateStudentProfileRequest)}.{error.Field}";

                ModelState.AddModelError(key, error.Description!);
            }

            if (IsAjax)
                return AjaxErrors();

            await LoadStudentInfoAsync(cancellationToken);
            return Page();
        }

        TempData["ToastSuccess"] = "Profil berhasil diperbarui.";

        if (IsAjax)
            return new JsonResult(new { redirect = Request.Path.Value });

        return RedirectToPage();
    }

    private async Task LoadStudentInfoAsync(
        CancellationToken cancellationToken)
    {
        var appUserId = _userContext.UserId;
        if (appUserId is null)
        {
            _toast.ToastError("Gagal", "Akun ini tidak ditemukan");
            return;
        }

        var data = await _studentProfileService.GetByAppUserIdAsync(appUserId, cancellationToken);
        if (data.IsFailure)
        {
            _toast.ToastError("Gagal", data.Error.Description!);
            return;
        }

        StudentProfileDetailInfo = data.Value!;
    }
}
