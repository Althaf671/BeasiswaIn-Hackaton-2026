using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Enums;
using BeasiswaIn.Web.Services.Students;

namespace BeasiswaIn.Web.Common.Errors;

internal static class StudentProfileServiceError
{
    private const string Domain = nameof(StudentProfileService);

    public static readonly Error AccountNotFound =
        Error.Create(
            ErrorType.NotFound,
            $"{Domain}.AccountNotFound",
            "Akun mu tidak ditemukan apakah sudah login?",
            Domain);

    public static readonly Error ThisIsNotYourAccount =
        Error.Create(
            ErrorType.Forbidden,
            $"{Domain}.ThisIsNotYourAccount",
            "Ini bukan akun mu woi",
            Domain);

    public static readonly Error NotFound =
        Error.Create(
            ErrorType.NotFound,
            $"{Domain}.NotFound",
            "Profil tidak ditemukan",
            Domain);
} 
