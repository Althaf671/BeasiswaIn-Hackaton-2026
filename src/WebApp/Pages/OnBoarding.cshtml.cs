using BeasiswaIn.Web.Common.Constants;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Scholarships;
using BeasiswaIn.Web.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages;

[Authorize]
public sealed class OnBoardingModel : PageModel
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    private readonly IScholarshipCategoryService _scholarshipCategoryService;
    private readonly IScholarshipService _scholarshipService;
    private readonly IOpenAiService _openAiService;
    private readonly IImageStorageService _imageStorageService;

    public IReadOnlyList<ScholarshipCategoryOptions> ScholarshipCategoryOptions { get; private set; } = [];
    public string ChatResult { get; private set; } = string.Empty;

    public OnBoardingModel(
        IScholarshipCategoryService scholarshipCategoryService,
        IScholarshipService scholarshipService,
        IOpenAiService openAiService,
        IImageStorageService imageStorageService)
    {
        _scholarshipCategoryService = scholarshipCategoryService;
        _scholarshipService = scholarshipService;
        _openAiService = openAiService;
        _imageStorageService = imageStorageService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var options = await _scholarshipCategoryService.GetOptionsAsync(cancellationToken);
        if (options.IsSuccess)
            ScholarshipCategoryOptions = options.Value!;

        return Page();
    }

    public async Task<IActionResult> OnPostAnalyzeAsync(
        UserScholarshipOnBoardingRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateCertificateAsync(request.Certificates, cancellationToken);
        if (!ModelState.IsValid)
            return ValidationErrors();

        var optionsResult = await _scholarshipCategoryService.GetOptionsAsync(cancellationToken);
        if (optionsResult.IsFailure)
            return Failure(optionsResult.Error.Description);

        var selectedIds = request.CategoryIds.Distinct().ToList();
        var selected = optionsResult.Value!
            .Where(o => selectedIds.Contains(o.Id))
            .ToList();

        if (selected.Count != selectedIds.Count)
        {
            ModelState.AddModelError(nameof(request.CategoryIds), "Ada kategori yang tidak valid.");
            return ValidationErrors();
        }

        var scholarshipsResult = await _scholarshipService
            .GetEligibleByCategoryIdsAsync(selectedIds, cancellationToken);
        if (scholarshipsResult.IsFailure)
            return Failure(scholarshipsResult.Error.Description);

        var imageResult = await _imageStorageService.UploadAsync(
            request.Certificates, 
            FileStorageConstant.FolderDirectory,
            cancellationToken);

        var extractionResult = await _openAiService.ExtractCertificateAsync(
            imageResult.Value!.Url,
            cancellationToken);

        ChatResult = scholarshipsResult.Value!.Count == 0
            ? """{"recommendations":[]}"""
            : await _openAiService.AnalyzeBestScholarshipChoices(
                request.Essay.Trim(),
                extractionResult,
                selected.Select(x => x.Name).ToList(),
                scholarshipsResult.Value!,
                cancellationToken);

        return new JsonResult(new
        {
            result = ChatResult,
            scholarships = scholarshipsResult.Value!.Select(scholarship => new
            {
                scholarship.Id,
                scholarship.Name,
                scholarship.Description,
                scholarship.TotalPrize,
                scholarship.ContentImageUrl,
                scholarship.GuideUrl,
                scholarship.RegistrationUrl,
                scholarship.Requirement,
                scholarship.ContactPerson,
                scholarship.RegistrationStartAt,
                scholarship.RegistrationEndAt,
                categories = optionsResult.Value!
                    .Where(category => scholarship.CategoryIds.Contains(category.Id))
                    .Select(category => category.Name)
            })
        });
    }

    // ---------- helpers ----------

    private async Task ValidateCertificateAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null) return; // sudah ditangani [Required]

        const string key = "Certificates";

        if (file.Length == 0 || file.Length > MaxFileSizeBytes)
            ModelState.AddModelError(key, $"Ukuran file maksimal {MaxFileSizeBytes / 1024 / 1024} MB.");
        else if (!AllowedContentTypes.Contains(file.ContentType))
            ModelState.AddModelError(key, "Hanya JPG, PNG, atau WebP.");
        else if (!await HasValidImageSignatureAsync(file, cancellationToken))
            ModelState.AddModelError(key, "Isi file bukan gambar JPG, PNG, atau WebP yang valid.");
    }

    private static async Task<bool> HasValidImageSignatureAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        if (read < header.Length) return false;

        var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var isWebp = header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);

        return isJpeg || isPng || isWebp;
    }

    private JsonResult ValidationErrors()
    {
        var errors = ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        return new JsonResult(new { errors }) { StatusCode = StatusCodes.Status422UnprocessableEntity };
    }

    // key "general" ditampilkan JS di bawah input sertifikat
    private static JsonResult Failure(string? message) =>
        new(new { errors = new Dictionary<string, string[]> { ["general"] = [message ?? "Terjadi kesalahan."] } })
        {
            StatusCode = StatusCodes.Status422UnprocessableEntity
        };
}