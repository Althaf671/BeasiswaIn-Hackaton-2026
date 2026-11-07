using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Models.Students;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Students;

internal sealed partial class LeaderboardService
{
    public async Task<Result<LeaderboardResponse>> GetAsync(
        string appUserId,
        CancellationToken cancellationToken)
    {
        var submissionCounts = _dbContext.SubmittedScholarships
            .AsNoTracking()
            .GroupBy(x => x.StudentId)
            .Select(x => new { StudentId = x.Key, Count = (int?)x.Count() });

        var profiles = await (
            from studentProfile in _dbContext.StudentProfiles.AsNoTracking()
            join submissions in submissionCounts on studentProfile.Id equals submissions.StudentId into counts
            from submissions in counts.DefaultIfEmpty()
            orderby studentProfile.Points descending, studentProfile.FullName, studentProfile.Id
            select new
            {
                studentProfile.FullName,
                studentProfile.Points,
                SubmissionCount = submissions.Count ?? 0,
                IsYou = studentProfile.AppUserId == appUserId
            })
            .ToListAsync(cancellationToken);

        var students = profiles.Select((profile, index) => new LeaderboardStudentResponse
        {
            Rank = index + 1,
            FullName = profile.FullName,
            Points = profile.Points,
            SubmissionCount = profile.SubmissionCount,
            IsYou = profile.IsYou
        }).ToList();

        var currentStudent = students.FirstOrDefault(x => x.IsYou);
        var submissionsToTopThree = currentStudent is { Rank: > 3 }
            ? (students[2].Points - currentStudent.Points) / SubmissionPoints + 1
            : 0;

        return Result<LeaderboardResponse>.Success(new LeaderboardResponse
        {
            Students = students,
            CurrentStudent = currentStudent,
            SubmissionsToTopThree = submissionsToTopThree
        });
    }
}
