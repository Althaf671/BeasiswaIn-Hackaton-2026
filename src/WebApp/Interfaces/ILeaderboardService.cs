using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Models.Students;

namespace BeasiswaIn.Web.Interfaces;

public interface ILeaderboardService
{
    public Task<Result<LeaderboardResponse>> GetAsync(
        string appUserId,
        CancellationToken cancellationToken);

    public Task<Result<string>> SubmitAsync(
        string appUserId,
        StudentSubmissionRequest request,
        CancellationToken cancellationToken);
}
