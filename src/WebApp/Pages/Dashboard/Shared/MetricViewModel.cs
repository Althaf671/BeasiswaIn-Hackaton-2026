

using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Pages.Dashboard.Shared;

public sealed record MetricViewModel
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? ValueLimit { get; init; }
    public string Icon { get; init; } = string.Empty;
    public MetricTrend Trend { get; init; }
    public string TrendValue { get; init; } = string.Empty;
}