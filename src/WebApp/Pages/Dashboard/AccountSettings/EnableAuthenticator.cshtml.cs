
using BeasiswaIn.Web.Infrastructures.DependencyInjection;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.FeatureManagement.Mvc;

namespace BeasiswaIn.Web.Pages.Dashboard.AccountSettings;

[FeatureGate(FeatureGateConstant.TwoFactor)]
[EnableRateLimiting(RateLimitRegistry.MediumRateAuthPolicy)]
public class EnableAuthenticatorModel : PageModel
{
    
}