using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Entities;
using BeasiswaIn.Web.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Personalizations;

internal sealed class UserScholarshipPreferenceService : IUserScholarshipPreferenceService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IScholarshipService _scholarshipService;
    private readonly IOpenAiService _openAiService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<UserScholarshipPreferenceService> _logger;

    public UserScholarshipPreferenceService(
        IApplicationDbContext dbContext,
        IScholarshipService scholarshipService,
        IOpenAiService openAiService,
        IImageStorageService imageStorageService,
        ILogger<UserScholarshipPreferenceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _scholarshipService = scholarshipService;
        _openAiService = openAiService;
        _imageStorageService = imageStorageService;
    }

    public async Task<bool> SaveScholarshipPreferencesAsync(
        string userId,
        List<RecommendedScholarship> recommendations,
        CancellationToken cancellationToken)
    {
        var requested = recommendations
            .Select(r => r.ScholarshipId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        if (requested.Count == 0)
            return false;

        var valid = await _dbContext.Scholarships
            .Where(x => requested.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (valid.Count == 0)
            return false;

        await using var tx = await _dbContext.BeginTransactionAsync(cancellationToken);

        await _dbContext.UserScholarshipPreferences
            .Where(x => x.StudentId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        _dbContext.UserScholarshipPreferences.AddRange(
            valid.Select(id => new UserScholarshipPreferences(userId, id)));

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return true;
    }
}