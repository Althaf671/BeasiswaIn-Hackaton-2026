using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Models.Dashboard;
using BeasiswaIn.Web.Models.Scholarships;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Scholarships;

internal sealed partial class ScholarshipService
{
    private const int TrendingMinScore = 5;
    private const int TrendingMaxItems = 10;

    public async Task<Result<IReadOnlyList<ScholarshipResponse>>> GetEligibleByCategoryIdsAsync(
        IReadOnlyCollection<string> categoryIds,
        CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0)
            return Result<IReadOnlyList<ScholarshipResponse>>.Success([]);

        var ids = categoryIds.Distinct().ToList();

        var scholarships = await _dbContext.Scholarships
            .AsNoTracking()
            .Where(s => s.IsEligible && !s.IsDeleted)
            .Where(s => _dbContext.ScholarshipCategoryAssignments
                .Any(a => a.ScholarshipId == s.Id && ids.Contains(a.CategoryId)))
            .Select(s => new ScholarshipResponse
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                TotalPrize = s.TotalPrize,
                ContentImageUrl = s.ContentImageUrl,
                CategoryIds = _dbContext.ScholarshipCategoryAssignments
                    .Where(assignment => assignment.ScholarshipId == s.Id)
                    .Select(assignment => assignment.CategoryId)
                    .ToList(),
                GuideUrl = s.GuideUrl,
                RegistrationUrl = s.RegistrationUrl,
                Requirement = s.Requirement,
                ContactPerson = s.ContactPerson,
                RegistrationStartAt = s.RegistrationStartAt,
                RegistrationEndAt = s.RegistrationEndAt
            })
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ScholarshipResponse>>.Success(scholarships);
    }

    public async Task<Result<ScholarshipListResponse>> GetAllAsync(
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var query = 
            from scholarship in _dbContext.Scholarships.AsNoTracking()
            orderby 
                scholarship.IsEligible descending,
                scholarship.Id
            select new ScholarshipResponse
            {
                Id = scholarship.Id,
                Name = scholarship.Name,
                Description = scholarship.Description,
                TotalPrize = scholarship.TotalPrize,
                ContentImageUrl = scholarship.ContentImageUrl,
                GuideUrl = scholarship.GuideUrl,
                RegistrationUrl = scholarship.RegistrationUrl,
                Requirement = scholarship.Requirement.ToList(),
                ContactPerson = scholarship.ContactPerson.ToList(),
                IsEligible = scholarship.IsEligible,
                RegistrationStartAt = scholarship.RegistrationStartAt,
                RegistrationEndAt = scholarship.RegistrationEndAt,
                IsBookmarked = scholarship.IsBookmarked,
                UpdatedAt = scholarship.UpdatedAt,
                CreatedAt = scholarship.CreatedAt
            };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<ScholarshipListResponse>.Success(
            new ScholarshipListResponse
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
    }

    public async Task<Result<ScholarshipListResponse>> GetAllTrendingAsync(
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var items = await (
            from scholarship in _dbContext.Scholarships.AsNoTracking()
            let score = scholarship.AiRecommendationOpenCount + scholarship.UserOpenCount
            where score >= TrendingMinScore
            orderby score descending, scholarship.Id
            select new ScholarshipResponse
            {
                Id = scholarship.Id,
                Name = scholarship.Name,
                Description = scholarship.Description,
                TotalPrize = scholarship.TotalPrize,
                ContentImageUrl = scholarship.ContentImageUrl,
                GuideUrl = scholarship.GuideUrl,
                RegistrationUrl = scholarship.RegistrationUrl,
                Requirement = scholarship.Requirement,
                ContactPerson = scholarship.ContactPerson,
                IsEligible = scholarship.IsEligible,
                RegistrationStartAt = scholarship.RegistrationStartAt,
                RegistrationEndAt = scholarship.RegistrationEndAt,
                IsBookmarked = scholarship.IsBookmarked,
                UpdatedAt = scholarship.UpdatedAt,
                CreatedAt = scholarship.CreatedAt
            })
            .Take(TrendingMaxItems)
            .ToListAsync(cancellationToken);

        return Result<ScholarshipListResponse>.Success(
            new ScholarshipListResponse
            {
                Items = items,
                Page = 1,
                PageSize = TrendingMaxItems,
                TotalCount = items.Count
            });
    }

    public async Task<Result<ScholarshipResponse>> GetByIdAsync(
        string id, 
        CancellationToken cancellationToken)
    {
        var categoryNames = await (
            from assignments in _dbContext.ScholarshipCategoryAssignments.AsNoTracking()
            join category in _dbContext.ScholarshipCategories.AsNoTracking()
                on assignments.CategoryId equals category.Id
            where 
                assignments.ScholarshipId == id &&
                category.IsActive
            select new 
            {
                Name = category.Name 
            })
            .ToListAsync(cancellationToken)
            ?? [];

        var response = await (
            from scholarship in _dbContext.Scholarships.AsNoTracking()
            where scholarship.Id == id
            select new ScholarshipResponse
            {
                Id = scholarship.Id,
                Name = scholarship.Name,
                Description = scholarship.Description,
                TotalPrize = scholarship.TotalPrize,
                ContentImageUrl = scholarship.ContentImageUrl,
                GuideUrl = scholarship.GuideUrl,
                RegistrationUrl = scholarship.RegistrationUrl,
                Requirement = scholarship.Requirement,
                ContactPerson = scholarship.ContactPerson,
                IsEligible = scholarship.IsEligible,
                CategoryIds = categoryNames.Select(x => x.Name).ToList(),
                RegistrationStartAt = scholarship.RegistrationStartAt,
                RegistrationEndAt = scholarship.RegistrationEndAt,
                UpdatedAt = scholarship.UpdatedAt,
                CreatedAt = scholarship.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            var error = ScholarshipServiceError.NotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<ScholarshipResponse>.Failure(error);
        }

        return Result<ScholarshipResponse>.Success(response);
    }

    public async Task<Result<ScholarshipListResponse>> GetAllByCategoryId(
        string categoryId, 
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var query = 
            from scholarship in _dbContext.Scholarships.AsNoTracking()
            where _dbContext.ScholarshipCategoryAssignments.Any(x => 
                x.CategoryId == categoryId &&
                x.ScholarshipId == scholarship.Id)
            orderby 
                scholarship.IsEligible descending,
                scholarship.Id
            select new ScholarshipResponse
            {
                Id = scholarship.Id,
                Name = scholarship.Name,
                Description = scholarship.Description,
                TotalPrize = scholarship.TotalPrize,
                ContentImageUrl = scholarship.ContentImageUrl,
                GuideUrl = scholarship.GuideUrl,
                RegistrationUrl = scholarship.RegistrationUrl,
                Requirement = scholarship.Requirement,
                ContactPerson = scholarship.ContactPerson,
                IsEligible = scholarship.IsEligible,
                RegistrationStartAt = scholarship.RegistrationStartAt,
                RegistrationEndAt = scholarship.RegistrationEndAt,
                IsBookmarked = scholarship.IsBookmarked,
                UpdatedAt = scholarship.UpdatedAt,
                CreatedAt = scholarship.CreatedAt
            };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<ScholarshipListResponse>.Success(
            new ScholarshipListResponse
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
    }

    public async Task<Result<MetricScholarshipResponse>> GetMetricAsync(
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(
            now.Year,
            now.Month,
            1,
            0, 0, 0,
            TimeSpan.Zero
        );

        var scholarships = _dbContext.Scholarships.AsNoTracking();

        var data = await (
            from root in _dbContext.ApplicationUsers.AsNoTracking().Take(1)
            select new MetricScholarshipResponse
            {
                TotalScholarship = new MetricResponse
                {
                    Value = scholarships.Count(),
                    TrendValue = scholarships.Count(x =>
                        x.CreatedAt >= monthStart)
                },

                TotalEligibleScholarship = new MetricResponse
                {
                    Value = scholarships.Count(x =>
                        x.IsEligible),

                    TrendValue = scholarships.Count(x =>
                        x.IsEligible && x.CreatedAt >= monthStart)
                },

                TotalScholarshipRegistrationOpened = new MetricResponse
                {
                    Value = scholarships.Count(x =>
                        x.RegistrationStartAt <= now &&
                        x.RegistrationEndAt >= now),

                    TrendValue = scholarships.Count(x =>
                        x.RegistrationStartAt <= now &&
                        x.RegistrationEndAt >= now &&
                        x.CreatedAt >= monthStart)
                },

                TotalScholarshipNotOpenedMetric = new MetricResponse
                {
                    Value = scholarships.Count(x =>
                        x.RegistrationStartAt > now),

                    TrendValue = scholarships.Count(x =>
                        x.RegistrationStartAt > now &&
                        x.CreatedAt >= monthStart)
                }
            }
        ).FirstOrDefaultAsync(cancellationToken)
        ?? new MetricScholarshipResponse();

        return Result<MetricScholarshipResponse>.Success(data);
    }

    public async Task<Result<ScholarshipListResponse>> GetLatestAsync(
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var since = now.AddDays(-15);

        var query =
            from scholarship in _dbContext.Scholarships.AsNoTracking()
            where scholarship.RegistrationStartAt >= since &&
                scholarship.RegistrationStartAt <= now
            orderby
                scholarship.RegistrationStartAt descending,
                scholarship.Id
            select new ScholarshipResponse
            {
                Id = scholarship.Id,
                Name = scholarship.Name,
                Description = scholarship.Description,
                TotalPrize = scholarship.TotalPrize,
                ContentImageUrl = scholarship.ContentImageUrl,
                GuideUrl = scholarship.GuideUrl,
                RegistrationUrl = scholarship.RegistrationUrl,
                Requirement = scholarship.Requirement,
                ContactPerson = scholarship.ContactPerson,
                IsEligible = scholarship.IsEligible,
                RegistrationStartAt = scholarship.RegistrationStartAt,
                RegistrationEndAt = scholarship.RegistrationEndAt,
                IsBookmarked = scholarship.IsBookmarked,
                UpdatedAt = scholarship.UpdatedAt,
                CreatedAt = scholarship.CreatedAt
            };

        var items = await query.ToListAsync(cancellationToken);

        return Result<ScholarshipListResponse>.Success(
            new ScholarshipListResponse
            {
                Items = items,
                Page = 1,
                PageSize = items.Count,
                TotalCount = items.Count
            });
    }

    public async Task<Result<ScholarshipListResponse>> GetMyRecommendedAsync(
        string appUserId,
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var studentProfileId = await (
            from studentProfile in _dbContext.StudentProfiles.AsNoTracking()
            where studentProfile.AppUserId == appUserId
            select studentProfile.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (studentProfileId is null)
        {
            var error = StudentProfileServiceError.NotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<ScholarshipListResponse>.Failure(error);
        }

        var query =
            from preference in _dbContext.UserScholarshipPreferences.AsNoTracking()
            join scholarship in _dbContext.Scholarships.AsNoTracking()
                on preference.ScholarshipId equals scholarship.Id
            where preference.StudentId == studentProfileId
            orderby
                scholarship.IsEligible descending,
                scholarship.Id
            select new ScholarshipResponse
            {
                Id = scholarship.Id,
                Name = scholarship.Name,
                Description = scholarship.Description,
                TotalPrize = scholarship.TotalPrize,
                ContentImageUrl = scholarship.ContentImageUrl,
                GuideUrl = scholarship.GuideUrl,
                RegistrationUrl = scholarship.RegistrationUrl,
                Requirement = scholarship.Requirement,
                ContactPerson = scholarship.ContactPerson,
                IsEligible = scholarship.IsEligible,
                RegistrationStartAt = scholarship.RegistrationStartAt,
                RegistrationEndAt = scholarship.RegistrationEndAt,
                IsBookmarked = scholarship.IsBookmarked,
                UpdatedAt = scholarship.UpdatedAt,
                CreatedAt = scholarship.CreatedAt
            };

        

        var items = await query.ToListAsync(cancellationToken);

        var result = await query.ToListAsync(cancellationToken);
        Console.WriteLine($"=== Preferensi student {studentProfileId}: {result.Count} item ===");

        for (var i = 0; i < result.Count; i++)
        {
            var s = result[i];
            Console.WriteLine(
                $"[{i + 1}] {s.Id} | {s.Name} | eligible={s.IsEligible} | " +
                $"{s.RegistrationStartAt:yyyy-MM-dd} s/d {s.RegistrationEndAt:yyyy-MM-dd}");
        }

        return Result<ScholarshipListResponse>.Success(
            new ScholarshipListResponse
            {
                Items = items,
                Page = 1,
                PageSize = items.Count,
                TotalCount = items.Count
            });
    }
}
