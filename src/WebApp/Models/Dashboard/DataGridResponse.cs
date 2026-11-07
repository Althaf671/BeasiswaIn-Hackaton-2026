
namespace BeasiswaIn.Web.Models.Dashboard;

public sealed record ScholarshipDataGridResponse
{
    public bool Categories { get; set; } = new();
    public bool Items {get; set; } = new();
}

public sealed record CompetitionDataGridResponse
{
    
}