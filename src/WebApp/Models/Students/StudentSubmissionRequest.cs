namespace BeasiswaIn.Web.Models.Students;

public sealed record StudentSubmissionRequest
{
    public string? ScholarshipSourceUrl { get; init; }
    
    public IFormFile? ScholarshipContentFile { get; init; }
}