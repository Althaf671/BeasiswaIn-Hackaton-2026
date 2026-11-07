using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Models.Competitions;
using BeasiswaIn.Web.Models.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Competitions;

internal sealed partial class CompetitionService
{
    private const int TrendingMinScore = 200;
    private const int TrendingMaxItems = 10;

    public async Task<Result<CompetitionListResponse>> GetAllAsync(
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var query = 
            from competition in _dbContext.Competitions.AsNoTracking()
            orderby 
                competition.IsEligible descending,
                competition.Id
            select new CompetitionResponse
            {
                Id = competition.Id,
                Name = competition.Name,
                Theme = competition.Theme,
                Description = competition.Description,
                TotalPrize = competition.TotalPrize,
                ContentImageUrl = competition.ContentImageUrl,
                GuideUrl = competition.GuideUrl,
                RegistrationUrl = competition.RegistrationUrl,
                Requirement = competition.Requirement.ToList(),
                ContactPerson = competition.ContactPerson.ToList(),
                IsEligible = competition.IsEligible,
                RegistrationStartAt = competition.RegistrationStartAt,
                RegistrationEndAt = competition.RegistrationEndAt,
                IsBookmarked = competition.IsBookmarked,
                UpdatedAt = competition.UpdatedAt,
                CreatedAt = competition.CreatedAt
            };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<CompetitionListResponse>.Success(
            new CompetitionListResponse
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
    }

    public async Task<Result<CompetitionListResponse>> GetAllTrendingAsync(
        CancellationToken cancellationToken)
    {
        var items = await (
            from competition in _dbContext.Competitions.AsNoTracking()
            let score = competition.AiRecommendationOpenCount + competition.UserOpenCount
            where score >= TrendingMinScore
            orderby score descending, competition.Id
            select new CompetitionResponse
            {
                Id = competition.Id,
                Name = competition.Name,
                Theme = competition.Theme,
                Description = competition.Description,
                TotalPrize = competition.TotalPrize,
                ContentImageUrl = competition.ContentImageUrl,
                GuideUrl = competition.GuideUrl,
                RegistrationUrl = competition.RegistrationUrl,
                Requirement = competition.Requirement,
                ContactPerson = competition.ContactPerson,
                IsEligible = competition.IsEligible,
                RegistrationStartAt = competition.RegistrationStartAt,
                RegistrationEndAt = competition.RegistrationEndAt,
                IsBookmarked = competition.IsBookmarked,
                UpdatedAt = competition.UpdatedAt,
                CreatedAt = competition.CreatedAt
            })
            .Take(TrendingMaxItems)
            .ToListAsync(cancellationToken);

        return Result<CompetitionListResponse>.Success(
            new CompetitionListResponse
            {
                Items = items,
                Page = 1,
                PageSize = TrendingMaxItems,
                TotalCount = items.Count
            });
    }

    public async Task<Result<CompetitionResponse>> GetByIdAsync(
        string id, 
        CancellationToken cancellationToken)
    {
        var categoryNames = await (
            from assignments in _dbContext.CompetitionCategoryAssignments.AsNoTracking()
            join category in _dbContext.CompetitionCategories.AsNoTracking()
                on assignments.CategoryId equals category.Id
            where 
                assignments.CompetitionId == id &&
                category.IsActive
            select new 
            {
                Name = category.Name 
            })
            .ToListAsync(cancellationToken)
            ?? [];

        var response = await (
            from competition in _dbContext.Competitions.AsNoTracking()
            where competition.Id == id
            select new CompetitionResponse
            {
                Id = competition.Id,
                Name = competition.Name,
                Theme = competition.Theme,
                Description = competition.Description,
                TotalPrize = competition.TotalPrize,
                ContentImageUrl = competition.ContentImageUrl,
                GuideUrl = competition.GuideUrl,
                RegistrationUrl = competition.RegistrationUrl,
                Requirement = competition.Requirement,
                ContactPerson = competition.ContactPerson,
                IsEligible = competition.IsEligible,
                CategoryIds = categoryNames.Select(x => x.Name).ToList(),
                RegistrationStartAt = competition.RegistrationStartAt,
                RegistrationEndAt = competition.RegistrationEndAt,
                UpdatedAt = competition.UpdatedAt,
                CreatedAt = competition.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            var error = CompetitionServiceError.NotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<CompetitionResponse>.Failure(error);
        }

        return Result<CompetitionResponse>.Success(response);
    }

    public Task<Result<CompetitionResponse>> GetBySlugAsync(
        string slug, 
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<CompetitionListResponse>> GetAllByCategoryId(
        string categoryId, 
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var query = 
            from competition in _dbContext.Competitions.AsNoTracking()
            where _dbContext.CompetitionCategoryAssignments.Any(x => 
                x.CategoryId == categoryId &&
                x.CompetitionId == competition.Id)
            orderby 
                competition.IsEligible descending,
                competition.Id
            select new CompetitionResponse
            {
                Id = competition.Id,
                Name = competition.Name,
                Theme = competition.Theme,
                Description = competition.Description,
                TotalPrize = competition.TotalPrize,
                ContentImageUrl = competition.ContentImageUrl,
                GuideUrl = competition.GuideUrl,
                RegistrationUrl = competition.RegistrationUrl,
                Requirement = competition.Requirement,
                ContactPerson = competition.ContactPerson,
                IsEligible = competition.IsEligible,
                RegistrationStartAt = competition.RegistrationStartAt,
                RegistrationEndAt = competition.RegistrationEndAt,
                IsBookmarked = competition.IsBookmarked,
                UpdatedAt = competition.UpdatedAt,
                CreatedAt = competition.CreatedAt
            };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<CompetitionListResponse>.Success(
            new CompetitionListResponse
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
    }

    public async Task<Result<MetricCompetitionResponse>> GetMetricAsync(
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

        var competitions = _dbContext.Competitions.AsNoTracking();

        var data = await (
            from root in _dbContext.ApplicationUsers.AsNoTracking().Take(1)
            select new MetricCompetitionResponse
            {
                TotalCompetition = new MetricResponse
                {
                    Value = competitions.Count(),
                    TrendValue = competitions.Count(x =>
                        x.CreatedAt >= monthStart)
                },

                TotalEligibleCompetition = new MetricResponse
                {
                    Value = competitions.Count(x =>
                        x.IsEligible),

                    TrendValue = competitions.Count(x =>
                        x.IsEligible && x.CreatedAt >= monthStart)
                },

                TotalCompetitionRegistrationOpened = new MetricResponse
                {
                    Value = competitions.Count(x =>
                        x.RegistrationStartAt <= now &&
                        x.RegistrationEndAt >= now),

                    TrendValue = competitions.Count(x =>
                        x.RegistrationStartAt <= now &&
                        x.RegistrationEndAt >= now &&
                        x.CreatedAt >= monthStart)
                },

                TotalCompetitionNotOpenedMetric = new MetricResponse
                {
                    Value = competitions.Count(x =>
                        x.RegistrationStartAt > now),

                    TrendValue = competitions.Count(x =>
                        x.RegistrationStartAt > now &&
                        x.CreatedAt >= monthStart)
                }
            }
        ).FirstOrDefaultAsync(cancellationToken)
        ?? new MetricCompetitionResponse();

        return Result<MetricCompetitionResponse>.Success(data);
    }
}