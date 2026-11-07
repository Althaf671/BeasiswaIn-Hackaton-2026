using AspNetCoreHero.ToastNotification.Abstractions;
using BeasiswaIn.Web.Interfaces;

namespace BeasiswaIn.Web.Pages.Dashboard.ScholarshipManagement;

public class ScholarshipPageModel : DashboardPageModel
{
    protected IScholarshipService _scholarshipService;
    protected IScholarshipCategoryService _scholarshipCategoryService;
    protected IUserScholarshipPreferenceService _userScholarshipPreferenceService;

    public ScholarshipPageModel(
        IScholarshipService scholarship,
        IScholarshipCategoryService scholarshipCategory,
        IUserScholarshipPreferenceService userScholarshipPreference,
        IUserContext userContext,
        INotyfService notifyService) : base(userContext, notifyService)
    {
        _scholarshipService = scholarship;
        _scholarshipCategoryService = scholarshipCategory;
        _userScholarshipPreferenceService = userScholarshipPreference;
    }
}