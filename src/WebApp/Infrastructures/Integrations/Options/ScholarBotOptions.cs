using System.ComponentModel.DataAnnotations;

namespace BeasiswaIn.Web.Infrastructures.Integrations.Options;

internal sealed class ScholarBotOptions
{
    public const string SectionName = "ScholarBot";
    public const string ClientKey = "ScholarBot";

    [Required]
    public string Model { get; init; } = "openai/gpt-6-luna";

    [Range(2, 10)]
    public int MaxIterations { get; init; } = 6;

    [Range(1, 12)]
    public int MaxToolCalls { get; init; } = 8;

    [Range(10, 180)]
    public int TimeoutSeconds { get; init; } = 90;

    [Range(256, 10000)]
    public int MaxOutputTokens { get; init; } = 6000;

    [Range(4000, 64000)]
    public int MaxTotalTokens { get; init; } = 48000;

    [Range(2000, 24000)]
    public int MaxToolResultCharacters { get; init; } = 16000;
}
