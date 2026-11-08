using System.Security.Claims;
using BeasiswaIn.Web.Infrastructures.Identity;
using BeasiswaIn.Web.Models.Students;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Common.Helpers;

public interface IStudentOnboardingState
{
    Task<bool> IsCompleteAsync(string accountId, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(string accountId, CreateStudentProfileRequest request,
        List<RecommendedScholarship> recommendations, CancellationToken cancellationToken);
}

// Evaluate Razor page metadata rather than public URLs: student pages have custom routes.
public sealed class StudentOnboardingMiddleware
{
    private readonly RequestDelegate _next;

    public StudentOnboardingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IStudentOnboardingState state)
    {
        var page = context.GetEndpoint()?.Metadata.GetMetadata<PageActionDescriptor>()?.ViewEnginePath;
        var onboarding = string.Equals(page, "/OnBoarding", StringComparison.OrdinalIgnoreCase);
        var legacyWizard = page?.StartsWith("/Students/Onboarding/", StringComparison.OrdinalIgnoreCase) == true;
        var studentPage = page?.StartsWith("/Students/", StringComparison.OrdinalIgnoreCase) == true;
        var landing = string.Equals(page, "/Index", StringComparison.OrdinalIgnoreCase);

        if (!onboarding && !studentPage && !landing)
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            // Landing page tetap publik untuk pengunjung yang belum login.
            if (landing)
            {
                await _next(context);
                return;
            }

            context.Response.Redirect("/identity/login?ReturnUrl=" + Uri.EscapeDataString(context.Request.Path + context.Request.QueryString));
            return;
        }

        if (context.User.IsInRole(AppRoles.Admin))
        {
            context.Response.Redirect("/dashboard");
            return;
        }

        var accountId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!context.User.IsInRole(AppRoles.Student) || string.IsNullOrEmpty(accountId))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var complete = await state.IsCompleteAsync(accountId, context.RequestAborted);
        if (legacyWizard || (onboarding && complete) || (!onboarding && !complete))
        {
            context.Response.Redirect(complete ? "/main-page" : "/onboarding");
            return;
        }

        await _next(context);
    }
}