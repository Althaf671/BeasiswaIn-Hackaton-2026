namespace BeasiswaIn.Web.Entities;

internal sealed class ScholarshipCategoryAssignments
{
    public string ScholarshipId { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;

    public ScholarshipCategoryAssignments(string scholarshipId, string categoryId)
    {
        ScholarshipId = scholarshipId;
        CategoryId = categoryId;
    }
}
