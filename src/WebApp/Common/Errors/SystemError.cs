using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Common.Errors;

internal static class SystemError
{
    private const string Domain = "System";

    internal static readonly Error InternalServerError =
        Error.Create(
            ErrorType.InternalServerError, 
            $"{Domain}.InternalServerError", 
            "Terjadi kesalahan pada sistem. Tim kami sedang memperbaikinya.", 
            Domain);

    internal static readonly Error ServiceUnavailable = 
        Error.Create(
            ErrorType.ServiceUnavailable, 
            $"{Domain}.ServiceUnavailable", 
            "Layanan sedang dalam pemeliharaan. Silakan coba lagi nanti.", 
            Domain);

    internal static Error NotFound(string resource) =>
        Error.Create(
            ErrorType.NotFound, 
            $"{Domain}.NotFound", 
            $"{resource} tidak ditemukan.", 
            Domain);

    internal static Error BadRequest(string detail) => 
        Error.Create(
            ErrorType.BadRequest, 
            $"{Domain}.BadRequest", 
            detail, 
            Domain);

    internal static readonly Error TooManyRequests =
        Error.Create(
            ErrorType.TooManyRequests, 
            $"{Domain}.TooManyRequests", 
            "Terlalu banyak permintaan. Harap santai sejenak.", 
            Domain);

    internal static readonly Error RequestTimeout =
        Error.Create(
            ErrorType.RequestTimeout, 
            $"{Domain}.RequestTimeout", 
            "Koneksi ke server terlalu lama. Silakan periksa koneksi Anda dan coba lagi.", 
            Domain);

    internal static readonly Error CsrfValidationFailed =
        Error.Create(
            ErrorType.BadRequest, 
            $"{Domain}.CsrfValidationFailed", 
            "Permintaan tidak valid, silakan muat ulang halaman dan coba lagi.", 
            Domain);
}