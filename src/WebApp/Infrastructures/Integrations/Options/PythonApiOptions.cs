using System.ComponentModel.DataAnnotations;

namespace BeasiswaIn.Web.Infrastructures.Integrations.Options;

public sealed class PythonApiOptions
{
    public const string SectionName = "PythonApi";

    [Required, Url]
    public string ChatEndpoint { get; init; } = string.Empty;

    // Backend-only token matching SCHOLARBOT_SERVICE_TOKEN on Python.
    public string ServiceToken { get; init; } = string.Empty;

    [Range(10, 300)]
    public int TimeoutSeconds { get; init; } = 90;
}
