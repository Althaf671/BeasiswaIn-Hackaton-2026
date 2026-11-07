using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Common.Errors;

internal static class ImageStorageError
{
    private const string Domain = "ImageStorageService";

    internal static readonly Error FileRequired =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.FileRequired",
            "File gambar wajib diunggah.",
            Domain);

    internal static readonly Error FileEmpty =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.FileEmpty",
            "File gambar tidak boleh kosong.",
            Domain);

    internal static readonly Error DirectoryRequired =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.DirectoryRequired",
            "Direktori penyimpanan gambar wajib ditentukan.",
            Domain);

    internal static readonly Error PublicIdRequired =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.PublicIdRequired",
            "Public ID gambar wajib ditentukan.",
            Domain);

    internal static readonly Error UploadFailed =
        Error.Create(
            ErrorType.InternalServerError,
            $"{Domain}.UploadFailed",
            "Gagal mengunggah gambar.",
            Domain);

    internal static readonly Error InvalidUploadResponse =
        Error.Create(
            ErrorType.InternalServerError,
            $"{Domain}.InvalidUploadResponse",
            "Penyimpanan gambar tidak mengembalikan informasi yang valid.",
            Domain);

    internal static readonly Error DeleteFailed =
        Error.Create(
            ErrorType.InternalServerError,
            $"{Domain}.DeleteFailed",
            "Gagal menghapus gambar dari penyimpanan.",
            Domain);
}