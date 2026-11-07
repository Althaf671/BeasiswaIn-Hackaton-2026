namespace BeasiswaIn.Web.Entities;

internal sealed class UserScholarshipPreferences
{
    public string UserId { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;

    public UserScholarshipPreferences(string userId, string categoryId)
    {
        UserId = userId;
        CategoryId = categoryId;
    }
}
