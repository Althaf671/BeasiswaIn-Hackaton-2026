using BeasiswaIn.Web.Infrastructures.Integrations.Options;
using BeasiswaIn.Web.Infrastructures.Integrations.Services;
using BeasiswaIn.Web.Interfaces;
using CloudinaryDotNet;
using Microsoft.Extensions.Options;

namespace BeasiswaIn.Web.Infrastructures.DependencyInjection;

internal static class FileStorageRegistry
{
    public static IServiceCollection AddAppFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<CloudinaryOption>()
            .Bind(configuration.GetSection(CloudinaryOption.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(x =>
        {
            var options = x.GetRequiredService<IOptions<CloudinaryOption>>().Value;

            var account = new Account(
                cloud: options.CloudName,
                apiKey: options.ApiKey,
                apiSecret: options.ApiSecret);

            return new Cloudinary(account);
        });

        services.AddScoped<IImageStorageService, ImageStorageService>();

        return services;
    }
}