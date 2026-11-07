using System.ComponentModel.DataAnnotations;
using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Constants;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Common.Validators;
using BeasiswaIn.Web.Entities;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Students;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Students;

internal sealed partial class LeaderboardService : ILeaderboardService
{
    private const int SubmissionPoints = 10;

    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<LeaderboardService> _logger;
    private readonly IImageStorageService _imageStorageService;

    public LeaderboardService(
        IApplicationDbContext dbContext,
        ILogger<LeaderboardService> logger,
        IImageStorageService imageStorageService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _imageStorageService = imageStorageService;
    }

    public async Task<Result<string>> SubmitAsync(
        string appUserId,
        StudentSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, true))
        {
            var errors = validationResults.Select(validation => LeaderboardServiceError.InvalidSubmission with
            {
                Description = validation.ErrorMessage,
                Field = validation.MemberNames.FirstOrDefault()
            });
            return Result<string>.Failure(errors);
        }

        var studentProfile = await _dbContext.StudentProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AppUserId == appUserId, cancellationToken);
        if (studentProfile is null)
        {
            var error = StudentProfileServiceError.NotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<string>.Failure(error);
        }

        string? contentUrl = null;
        string? contentPublicId = null;
        if (request.ScholarshipContentFile is not null)
        {
            var fileResult = await FileMimeValidator.ValidateAsync(
                request.ScholarshipContentFile,
                allowedMimeTypes: ["image/jpeg", "image/png", "image/webp"],
                cancellationToken);
            if (fileResult.IsFailure)
            {
                var error = fileResult.Error with { Field = nameof(request.ScholarshipContentFile) };
                _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
                return Result<string>.Failure(error);
            }

            var imageResult = await _imageStorageService.UploadAsync(
                file: request.ScholarshipContentFile,
                directory: FileStorageConstant.FolderDirectory,
                cancellationToken);
            if (imageResult.IsFailure)
            {
                var error = imageResult.Error with { Field = nameof(request.ScholarshipContentFile) };
                _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
                return Result<string>.Failure(error);
            }

            contentUrl = imageResult.Value!.Url;
            contentPublicId = imageResult.Value.PublicId;
        }

        var submission = new SubmittedScholarship(
            id: Ulid.NewUlid().ToString(),
            scholarshipSourceUrl: request.ScholarshipSourceUrl?.Trim(),
            scholarshipContentUrl: contentUrl,
            scholarshipContentPublicId: contentPublicId,
            studentId: studentProfile.Id);

        try
        {
            await using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

            // Increment in the database so concurrent submissions cannot overwrite each other's points.
            var updated = await _dbContext.StudentProfiles
                .Where(x => x.Id == studentProfile.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Points, x => x.Points + SubmissionPoints)
                    .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
            if (updated != 1)
                throw new DbUpdateException("Profil mahasiswa tidak ditemukan saat menyimpan kontribusi.");

            _dbContext.SubmittedScholarships.Add(submission);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result<string>.Success(submission.Id);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Gagal menyimpan kontribusi beasiswa untuk profil {StudentId}.", studentProfile.Id);

            if (contentPublicId is not null)
                await _imageStorageService.DeleteAsync(contentPublicId, CancellationToken.None);

            if (exception is OperationCanceledException)
                throw;

            return Result<string>.Failure(LeaderboardServiceError.SubmissionFailed);
        }
    }
}
