using System.ComponentModel.DataAnnotations;

namespace BeasiswaIn.Web.Models.Competitions;

public sealed record CompetitionCategoryOptions
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed record CompetitionCategoryResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int RelatedCompetitions { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; } 

    public string RelatedCompetitionText 
        => RelatedCompetitions == 0
            ? "Tidak ada"
            : $"{RelatedCompetitions} Lomba";
}

public sealed record CompetitionCategoryListResponse
{
    public IReadOnlyList<CompetitionCategoryResponse> Items { get; set; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages =>
        PageSize > 0
            ? (int)Math.Ceiling((double)TotalCount / PageSize)
            : 0;
}