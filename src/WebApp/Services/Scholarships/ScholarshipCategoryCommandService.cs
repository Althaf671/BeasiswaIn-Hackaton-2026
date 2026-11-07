using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Entities;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Scholarships;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Scholarships;

internal sealed partial class ScholarshipCategoryService : IScholarshipCategoryService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<ScholarshipCategoryService> _logger;

    public ScholarshipCategoryService(
        IApplicationDbContext dbContext,
        ILogger<ScholarshipCategoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<string>> CreateAsync(
        CreateScholarshipCategoryRequest request, 
        CancellationToken cancellationToken)
    {
        var nameDuplicate = await _dbContext.ScholarshipCategories
            .AnyAsync(x => x.Name == request.Name, cancellationToken);
        if (nameDuplicate)
        {
            var error = ScholarshipCategoryServiceError.NameDuplicated;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<string>.Failure(error);
        }

        var category = new ScholarshipCategory(
            id: Ulid.NewUlid().ToString(),
            name: request.Name.Trim().ToPascalCaseWithSpaces,
            isActive: request.IsActive);

        _dbContext.ScholarshipCategories.Add(category);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(category.Id);
    }

    public async Task<Result> DeleteAsync(
        string id, 
        CancellationToken cancellationToken)
    {
        var category = await _dbContext.ScholarshipCategories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null)
        {
            var error = ScholarshipCategoryServiceError.NotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<string>.Failure(error);
        }
        
        var inUse = await _dbContext.ScholarshipCategoryAssignments
            .AnyAsync(x => x.ScholarshipId == id, cancellationToken);
        if (inUse)
        {
            var error = ScholarshipCategoryServiceError.CurrentlyInUse;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<string>.Failure(error);
        }

        category.MarkItAsDeleted();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    public async Task<Result> UpdateAsync(
        string id,
        UpdateScholarshipCategoryRequest request, 
        CancellationToken cancellationToken)
    {
        var nameDuplicate = await _dbContext.ScholarshipCategories
            .AnyAsync(x => x.Id != id && x.Name == request.Name, cancellationToken);
        if (nameDuplicate)
        {
            var error = ScholarshipCategoryServiceError.NameDuplicated;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<string>.Failure(error);
        }

        var category = await _dbContext.ScholarshipCategories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null)
        {
            var error = ScholarshipCategoryServiceError.NotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result.Failure(error);
        }
        
        category.Name = request.Name;
        category.IsActive = request.IsActive;

        category.MarkItAsUpdated();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}