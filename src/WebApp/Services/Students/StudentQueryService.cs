using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Models.Students;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Students;

internal sealed partial class StudentProfileService
{
    public async Task<Result<StudentProfileResponse>> GetByAppUserIdAsync(
        string appUserId, 
        CancellationToken cancellationToken)
    {
        var response = await (
            from studentProfile in _dbContext.StudentProfiles.AsNoTracking()
            where studentProfile.AppUserId == appUserId
            select new StudentProfileResponse
            {
                Id = studentProfile.Id,
                FullName = studentProfile.FullName,
                Institution = studentProfile.Institution,
                FinalScore = studentProfile.FinalScore,
                Description = studentProfile.Description,
                Points = studentProfile.Points,
                AppUserId = studentProfile.AppUserId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            var error = StudentProfileServiceError.NotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<StudentProfileResponse>.Failure(error);
        }

        return Result<StudentProfileResponse>.Success(response);
    }
}