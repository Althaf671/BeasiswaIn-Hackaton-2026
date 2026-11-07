using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Enums;
using BeasiswaIn.Web.Services.Students;

namespace BeasiswaIn.Web.Common.Errors;

internal static class LeaderboardServiceError
{
    private const string Domain = nameof(LeaderboardService);

    public static readonly Error InvalidSubmission = Error.Create(
        ErrorType.BadRequest,
        nameof(InvalidSubmission),
        "Informasi beasiswa tidak valid.",
        Domain);

    public static readonly Error SubmissionFailed = Error.Create(
        ErrorType.InternalServerError,
        nameof(SubmissionFailed),
        "Informasi beasiswa gagal disimpan. Silakan coba lagi.",
        Domain);
}
