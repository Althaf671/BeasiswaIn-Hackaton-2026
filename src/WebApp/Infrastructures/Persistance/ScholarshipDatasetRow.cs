namespace BeasiswaIn.Web.Infrastructures.Persistance;

public sealed class ScholarshipDatasetRow
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal TotalPrize { get; init; }
    public string ContentImageUrl { get; init; } = string.Empty;
    public string GuideUrl { get; init; } = string.Empty;
    public string RegistrationUrl { get; init; } = string.Empty;
    public List<string> Requirement { get; init; } = [];
    public List<string> ContactPerson { get; init; } = [];
    public bool IsEligible { get; init; }
    public DateTimeOffset RegistrationStartAt { get; init; }
    public DateTimeOffset RegistrationEndAt { get; init; }
}