using System.Globalization;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Models.Dashboard;

namespace BeasiswaIn.Web.Models.Competitions;

public sealed record MetricCompetitionResponse
{
    public MetricResponse TotalCompetition { get; set; } = new();
    public MetricResponse TotalEligibleCompetition { get; set; } = new();
    public MetricResponse TotalCompetitionRegistrationOpened { get; set; } = new();
    public MetricResponse TotalCompetitionNotOpenedMetric { get; set; } = new();
}

public sealed record CompetitionResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Theme { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal TotalPrize { get; init; } 
    public string ContentImageUrl { get; init; } = string.Empty;
    public string GuideUrl { get; init; } = string.Empty;
    public string RegistrationUrl { get; init; } = string.Empty;
    public List<string> Requirement { get; init; } = [];
    public List<string> ContactPerson { get; init; } = [];
    public List<string> CategoryIds { get; init; } = [];
    public bool IsEligible { get; init; }
    public DateTimeOffset RegistrationStartAt { get; init; }
    public DateTimeOffset RegistrationEndAt { get; init; }
    public bool IsBookmarked { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public string TotalPrizeText => 
        $"{NumberFormatter.Rupiah(TotalPrize)}";

    public string RegistrationPeriod =>
        $"{RegistrationStartAt.ToString("d MMMM", new CultureInfo("id-ID"))} - " +
        $"{RegistrationEndAt.ToString("d MMMM, yyyy", new CultureInfo("id-ID"))}";
}

public sealed record CompetitionListResponse
{
    public IReadOnlyList<CompetitionResponse> Items { get; set; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages =>
        PageSize > 0
            ? (int)Math.Ceiling((double)TotalCount / PageSize)
            : 0;
}