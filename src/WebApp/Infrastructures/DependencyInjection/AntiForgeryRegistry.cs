

namespace BeasiswaIn.Web.Infrastructures.DependencyInjection;

internal static class AntiForgeryRegistry
{
    public static IServiceCollection AddAppAntiForgery(
        this IServiceCollection services,
        IWebHostEnvironment environment)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Name = environment.IsDevelopment() 
                ? "DEV-CSRF-TOKEN" 
                : "__PhotographyStudio-CSRF";
            options.Cookie.SecurePolicy = environment.IsDevelopment() 
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
        });

        return services;
    }
}