using Microsoft.AspNetCore.Mvc;

namespace BeasiswaIn.Web.Infrastructures.DependencyInjection;

internal static class CookieRegistry
{
    public static IServiceCollection AddCookie(
        this IServiceCollection services,
        IWebHostEnvironment environment)
    {
        services.Configure<CookieTempDataProviderOptions>(options =>
        {
            options.Cookie.Name = environment.IsDevelopment()
                ? "DEV-TEMPDATA"
                : "TEMPDATA";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        return services;
    }
}