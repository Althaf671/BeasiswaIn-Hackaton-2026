
using BeasiswaIn.Web.Infrastructures.DependencyInjection;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace BeasiswaIn.Web.Pages.Dashboard.AccountSettings;

[FeatureGate(FeatureGateConstant.ChangePassword)]
[EnableRateLimiting(RateLimitRegistry.MediumRateAuthPolicy)]
public class ChangePasswordModel : PageModel
{
    private readonly IFeatureManager _featureManager;

    public ChangePasswordModel(
        IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (await _featureManager.IsEnabledAsync(FeatureGateConstant.ChangePassword))
        {
            
        }
    }
}