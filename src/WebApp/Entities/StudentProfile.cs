using BeasiswaIn.Web.Common.Abstractions;

namespace BeasiswaIn.Web.Entities;

internal sealed class StudentProfile : EntityBase
{
    public string FullName { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public decimal? FinalScore { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Points { get; set; }
    public string AppUserId { get; set; } = string.Empty;

    public StudentProfile(
        string id,
        string fullName,
        string? institution,
        decimal? finalScore,
        string description,
        int points,
        string appUserId) : base(id)
    {
        FullName = fullName;
        Institution = institution;
        FinalScore = finalScore;
        Description = description;
        Points = points;
        AppUserId = appUserId;
    }

    public void MarkItAsUpdated()
    {
        MarkAsUpdated();
    }

    public void MarkItAsDeleted()
    {
        MarkAsDeleted();
    }
}