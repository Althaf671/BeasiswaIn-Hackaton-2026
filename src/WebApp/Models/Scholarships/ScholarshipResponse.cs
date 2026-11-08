using System.Globalization;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Models.Dashboard;

namespace BeasiswaIn.Web.Models.Scholarships;

public sealed record MetricScholarshipResponse
{
    public MetricResponse TotalScholarship { get; set; } = new();
    public MetricResponse TotalEligibleScholarship { get; set; } = new();
    public MetricResponse TotalScholarshipRegistrationOpened { get; set; } = new();
    public MetricResponse TotalScholarshipNotOpenedMetric { get; set; } = new();
}

public sealed record ScholarshipResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
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
        TotalPrize == 1000m ? string.Empty : NumberFormatter.Rupiah(TotalPrize);

    public string RegistrationPeriod =>
        $"{RegistrationStartAt.ToString("d MMMM", new CultureInfo("id-ID"))} - " +
        $"{RegistrationEndAt.ToString("d MMMM, yyyy", new CultureInfo("id-ID"))}";
}

public sealed record ScholarshipListResponse
{
    public IReadOnlyList<ScholarshipResponse> Items { get; set; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages =>
        PageSize > 0
            ? (int)Math.Ceiling((double)TotalCount / PageSize)
            : 0;
}