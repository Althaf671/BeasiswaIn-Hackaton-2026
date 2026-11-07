using AspNetCoreHero.ToastNotification.Abstractions;
using BeasiswaIn.Web.Interfaces;

namespace BeasiswaIn.Web.Pages.Dashboard.SubmitManagement;

public class SubmitManagementPageModel : DashboardPageModel
{
    public SubmitManagementPageModel(
        IUserContext userContext, 
        INotyfService notyfService) : base(userContext, notyfService)
    {
    }
}