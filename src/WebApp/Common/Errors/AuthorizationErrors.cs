using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Common.Errors;

internal static class AuthorizationErrors
{
    private const string Domain = "Authorization";

    internal static Error RoleMismatch(string role) =>
        Error.Create(
            ErrorType.Forbidden, 
            $"{Domain}.RoleMismatch", 
            $"Diperlukan role level: {role} untuk mengakses.", 
            Domain);

    internal static Error InsufficientPermission =
        Error.Create(
            ErrorType.Forbidden, 
            $"{Domain}.InsufficientPermission", 
            "Hak akses anda tidak cukup untuk melakukan tindakan ini.", 
            Domain);

    internal static Error CantAddClaimUserNotFound =
        Error.Create(
            ErrorType.NotFound, 
            $"{Domain}.CantAddClaimUserNotFound", 
            "Gagal memberi hak akses karena user tidak ditemukan.", 
            Domain);

    internal static Error FailedToAddOneOrMoreClaims =
        Error.Create(
            ErrorType.BadRequest, 
            $"{Domain}.FailedToAddOneOrMoreClaims", 
            "Gagal memberi satu atau lebih hak akses kepada user.", 
            Domain);

    internal static Error FailedToAssignRole =
        Error.Create(
            ErrorType.BadRequest, 
            $"{Domain}.FailedToAssignRole", 
            "Gagal memberi role kepada user.", 
            Domain);
}