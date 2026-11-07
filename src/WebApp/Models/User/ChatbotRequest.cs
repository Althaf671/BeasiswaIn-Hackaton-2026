using BeasiswaIn.Web.Models.Scholarships;

namespace BeasiswaIn.Web.Models.Chatbot;

public sealed record ScholarshipPersonlizationPromptRequest
{
    public string Role { get; init; } = string.Empty;
    public string StudentProfile { get; init; } = string.Empty;
    public string StudentPreference { get; init; } = string.Empty;
    public IReadOnlyList<string> SelectedScholarshipCategoryTags { get; init; } = [];
    public IReadOnlyList<ScholarshipResponse> SelectedScholarshipsBasedOnCategory { get; init; } = [];
    public string CertificateExtraction { get; init; } = string.Empty;
}