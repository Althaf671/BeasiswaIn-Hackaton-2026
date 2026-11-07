namespace BeasiswaIn.Web.Pages.Shared;

public sealed record ExploreCard
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string Emoji { get; init; } = "🎓";
    public string Match { get; init; } = string.Empty;
    public string Badge { get; init; } = string.Empty;
    public bool BadgeGreen { get; init; }
    public string Funding { get; init; } = string.Empty;
    public string Deadline { get; init; } = string.Empty;
    public string From { get; init; } = "#e6f4ed";
    public string To { get; init; } = "#cbebd9";
    public bool Sparkle { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
}