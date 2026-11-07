using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Constants;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Common.Validators;
using BeasiswaIn.Web.Entities;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Scholarships;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Scholarships;

internal sealed partial class ScholarshipService : IScholarshipService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<ScholarshipService> _logger;
    private readonly IImageStorageService _imageStorageService;

    public ScholarshipService(
        IApplicationDbContext dbContext,
        ILogger<ScholarshipService> logger,
        IImageStorageService imageStorageService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _imageStorageService = imageStorageService;
    }

    public async Task<Result<string>> CreateAsync(
        CreateScholarshipRequest request, 
        CancellationToken cancellationToken)
    {
        var errors = new List<Error>();
        
        var nameDuplicated = await _dbContext.Scholarships
            .AnyAsync(x => x.Name == request.Name, cancellationToken);
        if (nameDuplicated)
        {
            var error = ScholarshipServiceError.NameDuplicated;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            errors.Add(error);
        }

        if (errors.Count > 0)
            return Result<string>.Failure(errors);

        var fileResult = await FileMimeValidator.ValidateAsync(
            request.ContentImageFile,
            allowedMimeTypes: ["image/jpeg", "image/png", "image/webp"],
            cancellationToken);
        if (fileResult.IsFailure)
        {
            var error = fileResult.Error with { Field = "ContentImageFile" };
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<string>.Failure(error);
        }

        var imageResult = await _imageStorageService.UploadAsync(
            file: request.ContentImageFile, 
            directory: FileStorageConstant.FolderDirectory,
            cancellationToken);
        if (imageResult.IsFailure)
        {
            var error = imageResult.Error;
            _logger.LogInformation(message: "{Code} - {Message}", error.Code, error.Description);
            return Result<string>.Failure(error);
        }

        var scholarship = new Scholarship(
            id: Ulid.NewUlid().ToString(),
            name: request.Name.Trim().ToPascalCaseWithSpaces,
            description: request.Description.Trim(),
            totalPrize: request.TotalPrize,
            contentImageUrl: imageResult.Value!.Url,
            contentImagePublicId: imageResult.Value.PublicId,
            guideUrl: request.GuideUrl,
            registrationUrl: request.RegistrationUrl,
            requirement: request.Requirement,
            contactPerson: request.ContactPerson,
            isEligible: request.IsEligible,
            registrationStartAt: request.RegistrationStartAt.ToUniversalTime(),
            registrationEndAt: request.RegistrationEndAt.ToUniversalTime(),
            isBookmarked: false);

        _dbContext.Scholarships.Add(scholarship);
        
        foreach(var categoryIds in request.CategoryIds)
        {
            var assignment = new ScholarshipCategoryAssignments(
                scholarshipId: scholarship.Id,
                categoryId: categoryIds);

            _dbContext.ScholarshipCategoryAssignments.Add(assignment);
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(scholarship.Id);
    }

    public async Task<Result> DeleteAsync(
        string id, 
        CancellationToken cancellationToken)
    {
        var category = await _dbContext.Scholarships
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null)
        {
            var error = ScholarshipServiceError.NotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<string>.Failure(error);
        }

        await _dbContext.ScholarshipCategoryAssignments
            .Where(x => x.ScholarshipId == id)
            .ExecuteDeleteAsync(cancellationToken);

        category.MarkItAsDeleted();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    public async Task<Result> UpdateAsync(
        string id, 
        UpdateScholarshipRequest request, 
        CancellationToken cancellationToken)
    {
        var errors = new List<Error>();

        var scholarship = await _dbContext.Scholarships
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (scholarship is null)
        {
            var error = ScholarshipServiceError.NotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result.Failure(error);
        }

        var nameDuplicated = await _dbContext.Scholarships
            .AnyAsync(x => x.Id != id && x.Name == request.Name, cancellationToken);
        if (nameDuplicated)
        {
            var error = ScholarshipServiceError.NameDuplicated;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            errors.Add(error);
        }

        if (errors.Count > 0)
            return Result.Failure(errors);

        if (request.ContentImageFile is not null)
        {
            var fileResult = await FileMimeValidator.ValidateAsync(
                request.ContentImageFile,
                allowedMimeTypes: ["image/jpeg", "image/png", "image/webp"],
                cancellationToken);
            if (fileResult.IsFailure)
            {
                var error = fileResult.Error with { Field = "ContentImageFile" };
                _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
                return Result<string>.Failure(error);
            }

            var imageResult = await _imageStorageService.UploadAsync(
                file: request.ContentImageFile, 
                directory: FileStorageConstant.FolderDirectory,
                cancellationToken);
            if (imageResult.IsFailure)
            {
                var error = imageResult.Error;
                _logger.LogInformation(message: "{Code} - {Message}", error.Code, error.Description);
                return Result.Failure(error);
            }

            scholarship.ContentImageUrl = imageResult.Value!.Url;
            scholarship.ContentImagePublicId = imageResult.Value.PublicId;
        }

        scholarship.Name = request.Name.Trim().ToPascalCaseWithSpaces;
        scholarship.Description = request.Description.Trim();
        scholarship.TotalPrize = request.TotalPrize;
        scholarship.GuideUrl = request.GuideUrl;
        scholarship.RegistrationUrl = request.RegistrationUrl;
        scholarship.Requirement = request.Requirement;
        scholarship.ContactPerson = request.ContactPerson;
        scholarship.IsEligible = request.IsEligible;
        scholarship.RegistrationStartAt = request.RegistrationStartAt.ToUniversalTime();
        scholarship.RegistrationEndAt = request.RegistrationEndAt.ToUniversalTime();

        scholarship.MarkItAsUpdated();

        await _dbContext.ScholarshipCategoryAssignments
            .Where(x => x.ScholarshipId == id)
            .ExecuteDeleteAsync(cancellationToken);

        foreach(var categoryIds in request.CategoryIds)
        {
            var assignment = new ScholarshipCategoryAssignments(
                scholarshipId: scholarship.Id,
                categoryId: categoryIds);

            _dbContext.ScholarshipCategoryAssignments.Add(assignment);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}