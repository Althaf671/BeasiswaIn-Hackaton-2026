using System.Data;
using System.Security.Claims;
using BeasiswaIn.Web.Entities;
using BeasiswaIn.Web.Infrastructures.Identity;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Students;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Common.Helpers;

public interface IStudentOnboardingState
{
    Task<bool> IsCompleteAsync(string accountId, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(string accountId, CreateStudentProfileRequest request,
        List<RecommendedScholarship> recommendations, CancellationToken cancellationToken);
}

internal sealed class StudentOnboardingState(IApplicationDbContext database,
    IStudentProfileService profiles) : IStudentOnboardingState
{
    private const string Provider = "BeasiswaIn.Onboarding";
    private const string CompletionToken = "Completed.v1";
    private DbContext Context => (DbContext)database;

    public async Task<bool> IsCompleteAsync(string accountId, CancellationToken cancellationToken)
    {
        var profileIds = database.StudentProfiles.Where(p => p.AppUserId == accountId).Select(p => p.Id);
        if (!await profileIds.AnyAsync(cancellationToken)) return false;
        // Grandfather genuinely completed legacy profiles, not profile-only failed attempts.
        return await database.UserScholarshipPreferences.AnyAsync(p => profileIds.Contains(p.StudentId), cancellationToken)
            || await Context.Set<IdentityUserToken<string>>().AnyAsync(t => t.UserId == accountId
                && t.LoginProvider == Provider && t.Name == CompletionToken && t.Value == "true", cancellationToken);
    }

    public async Task<bool> CompleteAsync(string accountId, CreateStudentProfileRequest request,
        List<RecommendedScholarship> recommendations, CancellationToken cancellationToken)
    {
        // Serializable isolation prevents two first-time submissions from both inserting a profile.
        // A serialization failure rolls back; the client can safely retry the whole request.
        await using var transaction = await Context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (await IsCompleteAsync(accountId, cancellationToken)) return true;
        var result = await profiles.CreateAsync(request, cancellationToken);
        if (result.IsFailure) return false;
        var profileId = result.Value!;
        // Refuse a service result that belongs to any other account.
        if (!await database.StudentProfiles.AnyAsync(p => p.Id == profileId && p.AppUserId == accountId, cancellationToken))
            return false;
        await database.UserScholarshipPreferences.Where(p => p.StudentId == profileId).ExecuteDeleteAsync(cancellationToken);
        database.UserScholarshipPreferences.AddRange(recommendations.Select(r => r.ScholarshipId)
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(5).Select(id => new UserScholarshipPreferences(profileId, id)));
        var tokens = Context.Set<IdentityUserToken<string>>();
        var token = await tokens.FindAsync([accountId, Provider, CompletionToken], cancellationToken);
        if (token is null)
            tokens.Add(new IdentityUserToken<string> { UserId = accountId, LoginProvider = Provider, Name = CompletionToken, Value = "true" });
        else token.Value = "true";
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}


// Satu-satunya tempat yang memutuskan redirect onboarding/landing/halaman siswa.
public sealed class StudentOnboardingMiddleware
{
    private const string LoginPath = "/identity/login";
    private const string AdminPath = "/dashboard";
    private const string MainPath = "/main-page";
    private const string OnboardingPath = "/onboarding";

    private readonly RequestDelegate _next;

    public StudentOnboardingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IStudentOnboardingState state)
    {
        // Hanya Razor Page; static file dan controller/API tidak disentuh.
        var descriptor = context.GetEndpoint()?.Metadata.GetMetadata<PageActionDescriptor>();
        if (descriptor is null)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path;
        var viewPath = descriptor.ViewEnginePath ?? string.Empty;

        var isLanding = path.Equals("/", StringComparison.OrdinalIgnoreCase);
        var isOnboarding = path.StartsWithSegments(OnboardingPath, StringComparison.OrdinalIgnoreCase);
        var isLegacyWizard = viewPath.StartsWith("/Students/Onboarding/", StringComparison.OrdinalIgnoreCase);
        var isStudentPage = isLegacyWizard
            || viewPath.StartsWith("/Students/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments(MainPath, StringComparison.OrdinalIgnoreCase);

        if (!isLanding && !isOnboarding && !isStudentPage)
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            if (isLanding)
            {
                await _next(context); // landing tetap publik
                return;
            }

            context.Response.Redirect(LoginPath + "?ReturnUrl=" + Uri.EscapeDataString(path + context.Request.QueryString));
            return;
        }

        if (context.User.IsInRole(AppRoles.Admin))
        {
            context.Response.Redirect(AdminPath);
            return;
        }

        var accountId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!context.User.IsInRole(AppRoles.Student) || string.IsNullOrEmpty(accountId))
        {
            if (isLanding)
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var complete = await state.IsCompleteAsync(accountId, context.RequestAborted);

        // Submit onboarding kedua kali setelah selesai: tolak, jangan buat profil ganda.
        if (isOnboarding && complete && !HttpMethods.IsGet(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            return;
        }

        var mustRedirect = isLanding
            || isLegacyWizard
            || (isOnboarding && complete)
            || (isStudentPage && !complete);

        if (mustRedirect)
        {
            var target = complete ? MainPath : OnboardingPath;

            // Penjaga anti-loop: tidak pernah redirect ke halaman yang sedang dibuka.
            if (!path.StartsWithSegments(target, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect(target);
                return;
            }
        }

        await _next(context);
    }
}