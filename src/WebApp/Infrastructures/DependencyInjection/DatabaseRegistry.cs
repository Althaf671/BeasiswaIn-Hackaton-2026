

using Microsoft.EntityFrameworkCore;
using BeasiswaIn.Web.Infrastructures.Persistance;
using BeasiswaIn.Web.Infrastructures.Persistance.Exceptions;
using BeasiswaIn.Web.Interfaces;

namespace BeasiswaIn.Web.Infrastructures.DependencyInjection;

internal static class DatabaseRegistry
{
    public static IServiceCollection AddAppDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") 
            ?? throw new ArgumentException("Database connection not found.");
            
        services.AddDbContext<IApplicationDbContext, ApplicationDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .AddInterceptors(new PostgresExceptionTranslatorInterceptor()));
        
        return services;
    }
}