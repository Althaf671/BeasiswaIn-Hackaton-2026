using System.ComponentModel.DataAnnotations;

namespace BeasiswaIn.Web.Infrastructures.Integrations.Options;

internal sealed class ScholarBotOptions
{
    public const string SectionName = "ScholarBot";

    [Required]
    public string ServiceUrl { get; init; } = "http://127.0.0.1:8001";

    // Required: ScholarBot:ServiceToken (environment: ScholarBot__ServiceToken).
    // Must match Python's SCHOLARBOT_SERVICE_TOKEN; keep it in secrets, never browser configuration.
    [Required, MinLength(32)]
    public string ServiceToken { get; init; } = string.Empty;

    [Range(10, 180)]
    public int TimeoutSeconds { get; init; } = 100;

    [Range(256, 10000)]
    public int MaxMatchingOutputTokens { get; init; } = 6000;

    [Range(4000, 64000)]
    public int MaxMatchingTotalTokens { get; init; } = 48000;
}
