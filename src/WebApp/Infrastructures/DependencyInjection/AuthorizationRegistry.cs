

using BeasiswaIn.Web.Infrastructures.Identity;

namespace BeasiswaIn.Web.Infrastructures.DependencyInjection;

internal static class AuthorizationRegistry
{
    public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
                policy
                    .RequireAuthenticatedUser()
                    .RequireRole(AppRoles.Admin));
        });

        return services;
    }
}