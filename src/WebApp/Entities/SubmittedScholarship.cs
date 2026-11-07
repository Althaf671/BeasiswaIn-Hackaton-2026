using BeasiswaIn.Web.Common.Abstractions;

namespace BeasiswaIn.Web.Entities;

internal sealed class SubmittedScholarship : EntityBase
{
    public string? ScholarshipSourceUrl { get; set; }
    public string? ScholarshipContentUrl { get; set; }
    public string? ScholarshipContentPublicId { get; set; }
    public string StudentId { get; set; } = string.Empty;

    public SubmittedScholarship(
        string id,
        string? scholarshipSourceUrl,
        string? scholarshipContentUrl,
        string? scholarshipContentPublicId,
        string studentId) : base(id)
    {
        ScholarshipSourceUrl = scholarshipSourceUrl;
        ScholarshipContentUrl = scholarshipContentUrl;
        ScholarshipContentPublicId = scholarshipContentPublicId;
        StudentId = studentId;
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
