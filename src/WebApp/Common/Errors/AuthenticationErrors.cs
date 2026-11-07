using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Constants;
using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Common.Errors;

internal static class AuthenticationErrors
{
    private const string Domain = "Authentication";

    internal static string PasswordTooShort =>
        $"Kata sandi harus terdiri dari minimal {IdentityConstant.MinPasswordLength} karakter.";

    internal static string ComplexityRequirementsMessage =>
        "Kata sandi harus mengandung minimal satu huruf besar, satu huruf kecil, satu angka, dan satu karakter khusus.";

    internal static Error UserNotFound = 
        Error.Create(
            ErrorType.NotFound, 
            $"{Domain}.UserNotFound", 
            "User tidak ditemukan.", 
            Domain);

    internal static Error Unauthorized = 
        Error.Create(
            ErrorType.Unauthorized, 
            $"{Domain}.Unauthorized", 
            "Sesi anda tidak valid atau telah berakhir, Silakan login kembali.", 
            Domain);

    internal static Error Forbidden = 
        Error.Create(
            ErrorType.Forbidden, 
            $"{Domain}.Forbidden", 
            "Anda tidak memiliki izin untuk mengakses layanan ini.", 
            Domain);

    internal static Error InvalidCredentials = 
        Error.Create(
            ErrorType.Unauthorized, 
            $"{Domain}.InvalidCredentials", 
            "Email atau password yang anda masukan salah.", 
            Domain);

    internal static Error AccountLocked = 
        Error.Create(
            ErrorType.Forbidden, 
            $"{Domain}.AccountLocked", 
            "Akun terkunci karena anda terlalu banyak melakukan percobaan dan gagal, silakan tunggu beberapa saat atau hubungi admin.", 
            Domain);

    internal static Error EmailAlreadyExists = 
        Error.Create(
            ErrorType.Conflict, 
            $"{Domain}.EmailAlreadyExists", 
            "Gagal membuat akun karena user dengan email ini sudah ada.", 
            Domain);

    internal static Error FailedToCreateUserDueReasons = 
        Error.Create(
            ErrorType.BadRequest, 
            $"{Domain}.FailedToCreateUserDueReasons", 
            "Gagal membuat akun user.", 
            Domain);

    internal static Error FailedToDeactivateUser = 
        Error.Create(
            ErrorType.InternalServerError, 
            $"{Domain}.FailedToDeactivateUser", 
            "Gagal menonaktifkan akun user.", 
            Domain);

    internal static Error RequiresTwoFactor = 
        Error.Create(
            ErrorType.Unauthorized, 
            $"{Domain}.RequiresTwoFactor", 
            "Diperlukan verifikasi dua langkah untuk masuk.", 
            Domain);

    internal static Error UserNotAllowed = 
        Error.Create(
            ErrorType.Forbidden, 
            $"{Domain}.UserNotAllowed", 
            "Akun anda belum dapat digunakan untuk masuk. Pastikan email anda sudah dikonfirmasi.", 
            Domain);

    internal static Error TwoFactorSessionInvalid =
        Error.Create(
            ErrorType.Unauthorized,
            $"{Domain}.TwoFactorSessionInvalid",
            "Sesi verifikasi dua langkah tidak ditemukan atau telah berakhir, silakan login kembali.",
            Domain);

    internal static Error InvalidVerificationCode =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.InvalidVerificationCode",
            "Kode verifikasi tidak valid.",
            Domain);

    internal static Error InvalidAuthenticatorCode =
        Error.Create(
            ErrorType.Unauthorized,
            $"{Domain}.InvalidAuthenticatorCode",
            "Kode authenticator tidak valid.",
            Domain);

    internal static Error InvalidRecoveryCode =
        Error.Create(
            ErrorType.Unauthorized,
            $"{Domain}.InvalidRecoveryCode",
            "Kode pemulihan tidak valid.",
            Domain);

    internal static Error TwoFactorNotEnabled =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.TwoFactorNotEnabled",
            "Verifikasi dua langkah belum diaktifkan.",
            Domain);

    internal static Error FailedToUpdateTwoFactor =
        Error.Create(
            ErrorType.InternalServerError,
            $"{Domain}.FailedToUpdateTwoFactor",
            "Gagal mengubah pengaturan verifikasi dua langkah.",
            Domain);

    internal static Error InvalidOldPassword =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.InvalidOldPassword",
            "Kata sandi lama salah.",
            Domain);

    internal static Error FailedToChangePassword(string reasons) =>
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.FailedToChangePassword",
            reasons,
            Domain);

    internal static Error FailedToSetPhoneNumber =
        Error.Create(
            ErrorType.InternalServerError,
            $"{Domain}.FailedToSetPhoneNumber",
            "Gagal mengubah nomor telepon.",
            Domain);

    internal static Error EmailUnchanged =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.EmailUnchanged",
            "Email baru sama dengan email saat ini.",
            Domain);

    internal static Error EmailAlreadyInUse =
        Error.Create(
            ErrorType.Conflict,
            $"{Domain}.EmailAlreadyInUse",
            "Email tersebut sudah digunakan oleh akun lain.",
            Domain);

    internal static Error InvalidEmailChangeToken =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.InvalidEmailChangeToken",
            "Tautan konfirmasi perubahan email tidak valid atau sudah kedaluwarsa.",
            Domain);

    internal static Error FailedToChangeEmail =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.FailedToChangeEmail",
            "Gagal mengubah email. Tautan mungkin sudah kedaluwarsa.",
            Domain);
}