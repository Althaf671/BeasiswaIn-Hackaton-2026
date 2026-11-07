using System.ClientModel;
using BeasiswaIn.Web.Infrastructures.Integrations.Options;
using BeasiswaIn.Web.Infrastructures.Integrations.Services;
using BeasiswaIn.Web.Interfaces;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace BeasiswaIn.Web.Infrastructures.DependencyInjection;

internal static class OpenAiRegistry
{
    public static IServiceCollection AddAppOpenApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(x =>
        {
            var options = x.GetRequiredService<IOptions<OpenAiOptions>>().Value;

            return new ChatClient(
                model: options.Model,
                credential: new ApiKeyCredential(options.ApiKey),
                options: new OpenAI.OpenAIClientOptions
                {
                    NetworkTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                    Endpoint = new Uri(options.Endpoint)
                });
        });

        services.AddScoped<IOpenAiService, OpenAiService>();

        return services;
    }
}