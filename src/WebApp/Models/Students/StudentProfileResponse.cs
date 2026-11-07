namespace BeasiswaIn.Web.Models.Students;

public sealed record StudentProfileResponse
{
    public string Id { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Institution { get; init; }
    public decimal? FinalScore { get; init; }
    public string Description { get; init; } = string.Empty;
    public int Points { get; init; }
    public string AppUserId { get; init; } = string.Empty;
}
