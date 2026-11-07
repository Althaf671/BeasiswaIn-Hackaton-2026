using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Models.Scholarships;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Scholarships;

internal sealed partial class ScholarshipCategoryService
{
    public async Task<Result<ScholarshipCategoryListResponse>> GetAllAsync(
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var query = 
            from category in _dbContext.ScholarshipCategories.AsNoTracking()
            orderby
                category.Name descending,
                category.CreatedAt
            select new ScholarshipCategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive,
                RelatedScholarships = _dbContext.ScholarshipCategoryAssignments
                    .Count(x => x.CategoryId == category.Id),
                UpdatedAt = category.UpdatedAt,
                CreatedAt = category.CreatedAt
            };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<ScholarshipCategoryListResponse>.Success(
            new ScholarshipCategoryListResponse
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
    }

    public async Task<Result<ScholarshipCategoryResponse>> GetByIdAsync(
        string id, 
        CancellationToken cancellationToken)
    {
        var response = await (
            from category in _dbContext.ScholarshipCategories.AsNoTracking()
            where category.Id == id
            select new ScholarshipCategoryResponse
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
            var error = ScholarshipCategoryServiceError.NotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<ScholarshipCategoryResponse>.Failure(error);
        }

        return Result<ScholarshipCategoryResponse>.Success(response);
    }

    public async Task<Result<IReadOnlyList<ScholarshipCategoryOptions>>> GetOptionsAsync(
        CancellationToken cancellationToken)
    {
       return Result<IReadOnlyList<ScholarshipCategoryOptions>>.Success(
            await (
            from category in _dbContext.ScholarshipCategories.AsNoTracking()
            where category.IsActive
            select new ScholarshipCategoryOptions
            {
                Id = category.Id,
                Name = category.Name
            })
            .ToListAsync(cancellationToken));
    }
}