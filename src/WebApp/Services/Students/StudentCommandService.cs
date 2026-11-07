using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Entities;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Students;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Students;

internal sealed partial class StudentProfileService : IStudentProfileService
{
    private readonly IUserContext _userContext;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<StudentProfileService> _logger;

    public StudentProfileService(
        IApplicationDbContext dbContext,
        ILogger<StudentProfileService> logger,
        IUserContext userContext)
    {
        _dbContext = dbContext;
        _logger = logger;
        _userContext = userContext;
    }

    public async Task<Result<string>> CreateAsync(
        CreateStudentProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId;

        var account = await _dbContext.ApplicationUsers
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (account is null)
        {
            var error = StudentProfileServiceError.AccountNotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result<string>.Failure(error);
        }

        var existingId = await _dbContext.StudentProfiles
            .AsNoTracking()
            .Where(x => x.AppUserId == account.Id)
            .OrderByDescending(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId is not null)
        {
            await _dbContext.StudentProfiles
                .Where(x => x.Id == existingId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Institution, request.Institution)
                    .SetProperty(p => p.FinalScore, request.FinalScore)
                    .SetProperty(p => p.Description,
                        request.Description ?? "Belum mengisi essay bebas di onboarding."),
                    cancellationToken);

            return Result<string>.Success(existingId);
        }

        var studentProfile = new StudentProfile(
            id: Ulid.NewUlid().ToString(),
            fullName: account.UserName ?? "Unknown User",
            institution: request.Institution,
            finalScore: request.FinalScore,
            description: request.Description ?? "Belum mengisi essay bebas di onboarding.",
            points: 0,
            appUserId: account.Id);

        _dbContext.StudentProfiles.Add(studentProfile);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(studentProfile.Id);
    }

    public async Task<Result> UpdateAsync(
        string appUserId,
        UpdateStudentProfileRequest request,
        CancellationToken cancellationToken)
    {
        var studentProfile = await _dbContext.StudentProfiles
            .FirstOrDefaultAsync(x => x.AppUserId == appUserId, cancellationToken);
        if (studentProfile is null)
        {
            var error = StudentProfileServiceError.NotFound;
            _logger.LogWarning("{Code} - {Message}", error.Code, error.Description);
            return Result.Failure(error);
        }

        studentProfile.FullName = request.FullName.Trim();
        studentProfile.Institution = request.Institution?.Trim();
        studentProfile.FinalScore = request.FinalScore;
        studentProfile.Description = request.Description?.Trim() ?? "Tidak ada deskripsi baru di update";

        studentProfile.MarkItAsUpdated();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}