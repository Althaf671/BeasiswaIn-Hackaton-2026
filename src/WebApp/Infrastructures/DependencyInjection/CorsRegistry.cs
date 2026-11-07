

namespace BeasiswaIn.Web.Infrastructures.DependencyInjection;

internal static class CorsRegistry
{
    public const string PolicyName = "BeasiswaInCorsPolicy";

    public static IServiceCollection AddAppCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                    .WithHeaders("Content-Type", "X-CSRF-Token")
                    .WithMethods("GET", "POST", "PATCH", "PUT", "DELETE")
                    .AllowCredentials();
            });
        });

        return services;
    }
}