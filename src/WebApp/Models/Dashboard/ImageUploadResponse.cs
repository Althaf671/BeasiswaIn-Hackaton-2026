
namespace BeasiswaIn.Web.Models.Dashboard;

public sealed record class ImageUploadResponse
{
    public string Url { get; init; } = string.Empty;
    public string PublicId { get; init; } = string.Empty;
}