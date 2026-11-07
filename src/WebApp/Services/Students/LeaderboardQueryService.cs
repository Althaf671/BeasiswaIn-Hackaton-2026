using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace BeasiswaIn.Web.Services.Students;

internal sealed class LeaderboardService : ILeaderboardService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<LeaderboardService> _logger;

    private const int LeaderboardStudentLimit = 5;

    public LeaderboardService(
        IApplicationDbContext dbContext,
        ILogger<LeaderboardService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<StudentProfileResponse>>> GetBestStudent(
        CancellationToken cancellationToken)
    {
        var response = await (
            from studentProfile in _dbContext.StudentProfiles.AsNoTracking()
            orderby studentProfile.Points descending,
                studentProfile.FullName,
                studentProfile.Id
            select new StudentProfileResponse
            {
                FullName = studentProfile.FullName,
                Institution = studentProfile.Institution,
                FinalScore = studentProfile.FinalScore,
                Description = studentProfile.Description,
                Points = studentProfile.Points,
            })
            .Take(LeaderboardStudentLimit)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<StudentProfileResponse>>.Success(response);
    }

}