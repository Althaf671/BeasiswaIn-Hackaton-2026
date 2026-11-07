using System.ComponentModel.DataAnnotations;

namespace BeasiswaIn.Web.Models.Students;

public sealed record StudentSubmissionRequest : IValidatableObject
{
    [StringLength(2100, ErrorMessage = "Tautan beasiswa maksimal {1} karakter.")]
    public string? ScholarshipSourceUrl { get; init; }
    
    public IFormFile? ScholarshipContentFile { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasUrl = !string.IsNullOrWhiteSpace(ScholarshipSourceUrl);
        var hasFile = ScholarshipContentFile is not null;

        if (hasUrl == hasFile)
            yield return new ValidationResult("Pilih salah satu: tautan beasiswa atau gambar brosur.");

        if (hasUrl && (!Uri.TryCreate(ScholarshipSourceUrl!.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)))
        {
            yield return new ValidationResult(
                "Masukkan tautan beasiswa yang valid dengan http:// atau https://.",
                [nameof(ScholarshipSourceUrl)]);
        }
    }
}
