using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Models.Students;

namespace BeasiswaIn.Web.Interfaces;

public interface ILeaderboardService
{
    public Task<Result<IReadOnlyList<StudentProfileResponse>>> GetBestStudent(
        CancellationToken cancellationToken);
}