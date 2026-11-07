using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Constants;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Common.Validators;
using BeasiswaIn.Web.Entities;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Competitions;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Competitions;

internal sealed partial class CompetitionService : ICompetitionService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<CompetitionService> _logger;
    private readonly IImageStorageService _imageStorageService;

    public CompetitionService(
        IApplicationDbContext dbContext,
        ILogger<CompetitionService> logger,
        IImageStorageService imageStorageService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _imageStorageService = imageStorageService;
    }
    
    public async Task<Result<string>> CreateAsync(
        CreateCompetitionRequest request, 
        CancellationToken cancellationToken)
    {
        var errors = new List<Error>();

        var nameDuplicated = await _dbContext.Competitions
            .AnyAsync(x => x.Name == request.Name, cancellationToken);
        if (nameDuplicated)
        {
            var error = CompetitionServiceError.NameDuplicated;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            errors.Add(error);
        }

        var valid = request.CategoryIds.Distinct().ToList();
        var validCategoryCount = await _dbContext.CompetitionCategories
            .CountAsync(x => valid.Contains(x.Id), cancellationToken);

        if (validCategoryCount != valid.Count)
        {
            var error = CompetitionServiceError.CategoryNotFound;
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

        var competition = new Competition(
            id: Ulid.NewUlid().ToString(),
            name: request.Name.Trim().ToPascalCaseWithSpaces,
            theme: request.Theme.Trim().ToPascalCaseWithSpaces,
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

        _dbContext.Competitions.Add(competition);

        foreach(var categoryIds in request.CategoryIds)
        {
            var assignment = new CompetitionCategoryAssignments(
                competitionId: competition.Id,
                categoryId: categoryIds);

            _dbContext.CompetitionCategoryAssignments.Add(assignment);
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(competition.Id);
    }

    public async Task<Result> DeleteAsync(
        string id, 
        CancellationToken cancellationToken)
    {
        var category = await _dbContext.Competitions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null)
        {
            var error = CompetitionServiceError.NotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<string>.Failure(error);
        }

        await _dbContext.CompetitionCategoryAssignments
            .Where(x => x.CompetitionId == id)
            .ExecuteDeleteAsync(cancellationToken);

        category.MarkItAsDeleted();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    public async Task<Result> UpdateAsync(
        string id, 
        UpdateCompetitionRequest request, 
        CancellationToken cancellationToken)
    {
        var errors = new List<Error>();

        var competition = await _dbContext.Competitions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (competition is null)
        {
            var error = CompetitionServiceError.NotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result.Failure(error);
        }

        var nameDuplicated = await _dbContext.Competitions
            .AnyAsync(x => x.Id != id && x.Name == request.Name, cancellationToken);
        if (nameDuplicated)
        {
            var error = CompetitionServiceError.NameDuplicated;
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

            competition.ContentImageUrl = imageResult.Value!.Url;
            competition.ContentImagePublicId = imageResult.Value.PublicId;
        }

        competition.Name = request.Name.Trim().ToPascalCaseWithSpaces;
        competition.Theme = request.Theme.Trim().ToPascalCaseWithSpaces;
        competition.Description = request.Description.Trim();
        competition.TotalPrize = request.TotalPrize;
        competition.GuideUrl = request.GuideUrl;
        competition.RegistrationUrl = request.RegistrationUrl;
        competition.Requirement = request.Requirement;
        competition.ContactPerson = request.ContactPerson;
        competition.IsEligible = request.IsEligible;
        competition.RegistrationStartAt = request.RegistrationStartAt.ToUniversalTime();
        competition.RegistrationEndAt = request.RegistrationEndAt.ToUniversalTime();

        competition.MarkItAsUpdated();

        await _dbContext.CompetitionCategoryAssignments
            .Where(x => x.CompetitionId == id)
            .ExecuteDeleteAsync(cancellationToken);

        foreach(var categoryIds in request.CategoryIds)
        {
            var assignment = new CompetitionCategoryAssignments(
                competitionId: competition.Id,
                categoryId: categoryIds);

            _dbContext.CompetitionCategoryAssignments.Add(assignment);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}