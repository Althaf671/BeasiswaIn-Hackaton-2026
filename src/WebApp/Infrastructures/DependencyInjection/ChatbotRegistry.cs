using BeasiswaIn.Web.Infrastructures.Integrations.Options;
using Microsoft.Extensions.Options;

namespace BeasiswaIn.Web.Infrastructures.DependencyInjection;

internal static class ChatbotRegistry
{
    public static IServiceCollection AddAppChatbot(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PythonApiOptions>()
            .Bind(configuration.GetSection(PythonApiOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options =>
                Uri.TryCreate(options.ChatEndpoint, UriKind.Absolute, out var endpoint) &&
                (endpoint.Scheme == Uri.UriSchemeHttp || endpoint.Scheme == Uri.UriSchemeHttps),
                "PythonApi:ChatEndpoint harus berupa URL HTTP atau HTTPS yang valid.")
            .ValidateOnStart();

        services.AddHttpClient("PythonChat", (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<PythonApiOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }
}
