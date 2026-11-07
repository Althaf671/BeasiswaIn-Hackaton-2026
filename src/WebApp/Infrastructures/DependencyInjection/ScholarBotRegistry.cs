using BeasiswaIn.Web.Infrastructures.Integrations.Options;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Services.ScholarBot;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
            .Validate(options => Uri.TryCreate(options.ServiceUrl, UriKind.Absolute, out var uri) &&
                uri.Scheme is "http" or "https" && string.IsNullOrEmpty(uri.UserInfo) &&
                string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment),
                "ScholarBot:ServiceUrl must be an absolute HTTP(S) service URL without credentials, query or fragment.")
            .Validate(options => options.ServiceToken is { Length: >= 32 } &&
                options.ServiceToken.All(character => character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or
                    >= '0' and <= '9' or '-' or '_'),
                "ScholarBot:ServiceToken is required and must contain at least 32 ASCII URL-safe characters; " +
                "configure ScholarBot__ServiceToken to match Python's SCHOLARBOT_SERVICE_TOKEN.")
            .Validate(options => options.MaxMatchingOutputTokens <= options.MaxMatchingTotalTokens,
                "ScholarBot matching output limit must not exceed the matching total token budget.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddDataProtection();
        services.AddSingleton<ScholarBotDelegation>();
        services.AddScoped<ScholarBotUsage>();
        services.AddScoped<IScholarBotTools, ScholarBotTools>();
        services.AddHttpClient<IScholarBotService, ScholarBotClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ScholarBotOptions>>().Value;
            client.BaseAddress = new Uri(options.ServiceUrl.TrimEnd('/') + "/");
            // The linked per-request deadline also covers streaming the bounded response body.
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.MaxResponseContentBufferSize = 1048576;
        })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            })
            // No retry/resilience handlers: a failed request must never duplicate paid work.
            // Remove HTTP logging entirely, including trace-level headers and delegation tokens.
            .RemoveAllLoggers();
        return services;
    }
}
