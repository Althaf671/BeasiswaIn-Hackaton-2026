namespace BeasiswaIn.Web.Entities;

internal sealed class UserScholarshipPreferences
{
    public string StudentId { get; set; } = string.Empty;

    public string ScholarshipId { get; set; } = string.Empty;

    public UserScholarshipPreferences(string studentId, string scholarshipId)
    {
        StudentId = studentId;
        ScholarshipId = scholarshipId;
    }
}
