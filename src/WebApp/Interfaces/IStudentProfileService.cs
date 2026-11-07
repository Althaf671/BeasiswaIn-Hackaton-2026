using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Models.Students;

namespace BeasiswaIn.Web.Interfaces;

public interface IStudentProfileService
{
    public Task<Result<string>> CreateAsync(
        CreateStudentProfileRequest request, 
        CancellationToken cancellationToken);

    public Task<Result<StudentProfileResponse>> GetByAppUserIdAsync(
        string appUserId,
        CancellationToken cancellationToken);

    public Task<Result> UpdateAsync(
        string id,
        UpdateStudentProfileRequest request, 
        CancellationToken cancellationToken);
}