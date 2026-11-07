namespace BeasiswaIn.Web.Models.Scholarships;

public sealed record ScholarshipCategoryOptions
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed record ScholarshipCategoryResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int RelatedScholarships { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; } 

    public string RelatedScholarshipText
        => $"{RelatedScholarships} Beasiswa";
}

public sealed record ScholarshipCategoryListResponse
{
    public IReadOnlyList<ScholarshipCategoryResponse> Items { get; set; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages =>
        PageSize > 0
            ? (int)Math.Ceiling((double)TotalCount / PageSize)
            : 0;
}