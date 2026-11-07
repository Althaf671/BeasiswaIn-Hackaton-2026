using System.ClientModel;
using System.ClientModel.Primitives;
using BeasiswaIn.Web.Infrastructures.Integrations.Options;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Services.ScholarBot;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace BeasiswaIn.Web.Infrastructures.DependencyInjection;

internal static class ScholarBotRegistry
{
    public static IServiceCollection AddAppScholarBot(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ScholarBotOptions>()
            .Bind(configuration.GetSection(ScholarBotOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => !string.IsNullOrWhiteSpace(options.Model),
                "ScholarBot model must not be blank.")
            .Validate(options => options.MaxOutputTokens <= options.MaxTotalTokens,
                "ScholarBot output limit must not exceed the total token budget.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddKeyedSingleton<ChatClient>(ScholarBotOptions.ClientKey, (provider, _) =>
        {
            var options = provider.GetRequiredService<IOptions<ScholarBotOptions>>().Value;
            var providerOptions = provider.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            return new ChatClient(
                model: options.Model,
                credential: new ApiKeyCredential(providerOptions.ApiKey),
                options: new OpenAI.OpenAIClientOptions
                {
                    Endpoint = new Uri(providerOptions.Endpoint),
                    NetworkTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                    RetryPolicy = new ClientRetryPolicy(0),
                    ClientLoggingOptions = new ClientLoggingOptions
                    {
                        EnableLogging = false,
                        EnableMessageLogging = false,
                        EnableMessageContentLogging = false
                    }
                });
        });
        services.AddScoped<ScholarBotUsage>();
        services.AddScoped<IScholarBotTools, ScholarBotTools>();
        services.AddScoped<IScholarBotService, ScholarBotService>();
        return services;
    }
}
