using System.ComponentModel.DataAnnotations;

namespace BeasiswaIn.Web.Infrastructures.Integrations.Options;

internal sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required]
    public string Model { get; init; } = "gpt-4o-mini";

    [Required, Url]
    public string Endpoint { get; init; } = "https://ai-gateway.vercel.sh/v1";

    [Range(10, 300)]
    public int TimeoutSeconds { get; init; } = 60;
}