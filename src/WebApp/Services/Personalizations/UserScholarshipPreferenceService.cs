using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Entities;
using BeasiswaIn.Web.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Personalizations;

internal sealed class UserScholarshipPreferenceService : IUserScholarshipPreferenceService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICompetitionService _competitionService;
    private readonly IScholarshipService _scholarshipService;
    private readonly IOpenAiService _openAiService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<UserScholarshipPreferenceService> _logger;

    public UserScholarshipPreferenceService(
        IApplicationDbContext dbContext,
        ICompetitionService competitionService,
        IScholarshipService scholarshipService,
        IOpenAiService openAiService,
        IImageStorageService imageStorageService,
        ILogger<UserScholarshipPreferenceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _competitionService = competitionService;
        _scholarshipService = scholarshipService;
        _openAiService = openAiService;
        _imageStorageService = imageStorageService;
    }

    public async Task SaveScholarshipPreferencesAsync(
        string userId, 
        IReadOnlyCollection<string> categoryIds, 
        CancellationToken cancellationToken)
    {
        var requested = categoryIds.Distinct().Take(15).ToList();

        var valid = await _dbContext.ScholarshipCategories
            .Where(x => requested.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        await using var tx = await _dbContext.BeginTransactionAsync(cancellationToken);

        await _dbContext.UserScholarshipPreferences
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        _dbContext.UserScholarshipPreferences.AddRange(
            valid.Select(id => new UserScholarshipPreferences(userId, id)));

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }
}