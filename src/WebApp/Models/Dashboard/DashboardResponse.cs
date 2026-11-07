
using BeasiswaIn.Web.Common.Helpers;

namespace BeasiswaIn.Web.Models.Dashboard;

public sealed record DashboardResponse
{
    // Registration deadlines
    public MetricResponse TotalCompetitionClosingSoon { get; init; } = new();
    public MetricResponse TotalScholarshipClosingSoon { get; init; } = new();

    public MetricResponse TotalExpiredCompetition { get; init; } = new();
    public MetricResponse TotalExpiredScholarship { get; init; } = new();

    // Content activity
    public MetricResponse TotalCompetitionCreatedThisMonth { get; init; } = new();
    public MetricResponse TotalScholarshipCreatedThisMonth { get; init; } = new();

    // Categories
    public MetricResponse TotalCompetitionCategory { get; init; } = new();
    public MetricResponse TotalScholarshipCategory { get; init; } = new();

    public DashboardChartResponse ChartData { get; init; } = new();
}

public sealed record MetricResponse
{
    public long Value { get; init; } 
    public long ValueLimit { get; init; } 
    public long TrendValue { get; init; }
}

public sealed record DashboardChartResponse
{
    public int InquiryIncompleteCount { get; init; }
    public int InquiryCompletedCount { get; init; }
    public int PhotographyPackageChoosenCount { get; init; }
    public int VisitorCount { get; init; }
    public List<(DateOnly Date, int Value)> ThisWeekRecentVisitor { get; init; } = [];
}

public sealed record AuditLogResponse
{
    public string Id { get; init; } = string.Empty;
    public string ActorUserNameUnformmated { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? IpAdress { get; init; }
    public DateTimeOffset OccuredOn { get; init; }

    public string ActorUserName =>
        ActorUserNameUnformmated.ToPascalCaseWithSpaces;
}

public sealed record AuditLogListResponse
{
    public IReadOnlyList<AuditLogResponse> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages =>
        PageSize > 0
            ? (int)Math.Ceiling((double)TotalCount / PageSize)
            : 0;
}