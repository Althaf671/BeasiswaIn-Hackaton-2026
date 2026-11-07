using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Entities;
using BeasiswaIn.Web.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Personalizations;

internal sealed class UserCompetitionPreferenceService : IUserCompetitionPreferenceService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<UserCompetitionPreferenceService> _logger;

    public UserCompetitionPreferenceService(
        IApplicationDbContext dbContext,
        ILogger<UserCompetitionPreferenceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SaveCompetitionPreferencesAsync(
        string userId, 
        IReadOnlyCollection<string> categoryIds, 
        CancellationToken cancellationToken)
    {
        var requested = categoryIds.Distinct().Take(15).ToList();

        var valid = await _dbContext.CompetitionCategories
            .Where(x => requested.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        await using var tx = await _dbContext.BeginTransactionAsync(cancellationToken);

        await _dbContext.UserScholarshipPreferences
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        _dbContext.UserCompetitionPreferences.AddRange(
            valid.Select(id => new UserCompetitionPreferences(userId, id)));

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }
}