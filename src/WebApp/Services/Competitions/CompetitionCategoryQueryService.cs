using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Models.Competitions;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Competitions;

internal sealed partial class CompetitionCategoryService
{
    public async Task<Result<CompetitionCategoryListResponse>> GetAllAsync(
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var query = 
            from category in _dbContext.CompetitionCategories.AsNoTracking()
            orderby
                category.Name descending,
                category.CreatedAt
            select new CompetitionCategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive,
                RelatedCompetitions = _dbContext.CompetitionCategoryAssignments
                    .Count(x => x.CategoryId == category.Id),
                UpdatedAt = category.UpdatedAt,
                CreatedAt = category.CreatedAt
            };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<CompetitionCategoryListResponse>.Success(
            new CompetitionCategoryListResponse
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
    }

    public async Task<Result<CompetitionCategoryResponse>> GetByIdAsync(
        string id, 
        CancellationToken cancellationToken)
    {
        var response = await (
            from category in _dbContext.CompetitionCategories.AsNoTracking()
            where category.Id == id
            select new CompetitionCategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive,
                UpdatedAt = category.UpdatedAt,
                CreatedAt = category.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            var error = CompetitionCategoryServiceError.NotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<CompetitionCategoryResponse>.Failure(error);
        }

        return Result<CompetitionCategoryResponse>.Success(response);
    }

    public async Task<Result<IReadOnlyList<CompetitionCategoryOptions>>> GetOptionsAsync(
        CancellationToken cancellationToken)
    {
       return Result<IReadOnlyList<CompetitionCategoryOptions>>.Success(
            await (
            from category in _dbContext.CompetitionCategories.AsNoTracking()
            where category.IsActive
            select new CompetitionCategoryOptions
            {
                Id = category.Id,
                Name = category.Name
            })
            .ToListAsync(cancellationToken));
    }
}