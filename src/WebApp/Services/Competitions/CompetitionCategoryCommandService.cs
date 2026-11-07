using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Entities;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Competitions;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Competitions;

internal sealed partial class CompetitionCategoryService : ICompetitionCategoryService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<CompetitionCategoryService> _logger;

    public CompetitionCategoryService(
        IApplicationDbContext dbContext,
        ILogger<CompetitionCategoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<string>> CreateAsync(
        CreateCompetitionCategoryRequest request, 
        CancellationToken cancellationToken)
    {
        var nameDuplicate = await _dbContext.CompetitionCategories
            .AnyAsync(x => x.Name == request.Name, cancellationToken);
        if (nameDuplicate)
        {
            var error = CompetitionCategoryServiceError.NameDuplicated;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<string>.Failure(error);
        }

        var category = new CompetitionCategory(
            id: Ulid.NewUlid().ToString(),
            name: request.Name.Trim().ToPascalCaseWithSpaces,
            isActive: request.IsActive);

        _dbContext.CompetitionCategories.Add(category);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(category.Id);
    }

    public async Task<Result> DeleteAsync(
        string id, 
        CancellationToken cancellationToken)
    {
        var category = await _dbContext.CompetitionCategories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null)
        {
            var error = CompetitionCategoryServiceError.NotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<string>.Failure(error);
        }
        
        var inUse = await _dbContext.CompetitionCategoryAssignments
            .AnyAsync(x => x.CategoryId == id, cancellationToken);
        if (inUse)
        {
            var error = CompetitionCategoryServiceError.CurrentlyInUse;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<string>.Failure(error);
        }

        category.MarkItAsDeleted();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    public async Task<Result> UpdateAsync(
        string id,
        UpdateCompetitionCategoryRequest request, 
        CancellationToken cancellationToken)
    {
        var nameDuplicate = await _dbContext.CompetitionCategories
            .AnyAsync(x => x.Id != id && x.Name == request.Name, cancellationToken);
        if (nameDuplicate)
        {
            var error = CompetitionCategoryServiceError.NameDuplicated;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<string>.Failure(error);
        }

        var category = await _dbContext.CompetitionCategories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null)
        {
            var error = CompetitionCategoryServiceError.NotFound;
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