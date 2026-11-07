using System.ComponentModel.DataAnnotations;

namespace BeasiswaIn.Web.Models.ScholarBot;

public sealed record ScholarBotRequest
{
    public const int MaxMessageCharacters = 12000;
    public const int MaxHistoryMessageCharacters = 24000;
    public const int MaxContextCharacters = 48000;
    public const int MaxHistoryMessages = 12;

    [Required, StringLength(MaxMessageCharacters, MinimumLength = 1)]
    public string Message { get; init; } = string.Empty;

    [MaxLength(MaxHistoryMessages)]
    public List<ScholarBotHistoryMessage> History { get; init; } = [];
}

public sealed record ScholarBotHistoryMessage
{
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}

public sealed record ScholarBotResponse
{
    public string Answer { get; init; } = string.Empty;
    public IReadOnlyList<ScholarBotSource> Sources { get; init; } = [];
    public IReadOnlyList<ScholarBotToolTrace> ToolCalls { get; init; } = [];
    public ScholarBotUsageSummary Usage { get; init; } = new(0, 0, 0, 0);
}

public sealed record ScholarBotSource
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? RegistrationUrl { get; init; }
    public string? GuideUrl { get; init; }
    public DateTimeOffset Deadline { get; init; }
}

public sealed record ScholarBotToolTrace(string Name, bool Success, long DurationMilliseconds);
public sealed record ScholarBotUsageSummary(int InputTokens, int OutputTokens, int ApiCalls, decimal? CostUsd);
public sealed record ScholarBotToolResult(string Content, bool Success, IReadOnlyList<ScholarBotSource> Sources);
