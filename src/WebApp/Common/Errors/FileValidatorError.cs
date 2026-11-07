using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Enums;
using BeasiswaIn.Web.Common.Validators;

namespace BeasiswaIn.Web.Common.Errors;

internal static class FileMimeValidatorError
{
    private const string Domain = nameof(FileMimeValidator);

    public static readonly Error FileRequired =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.FileRequired",
            "File wajib diunggah",
            Domain) with { Field = "ContentImageFile" };

    public static readonly Error FileTooLarge =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.FileTooLarge",
            "Ukuran file melebihi batas yang diizinkan",
            Domain) with { Field = "ContentImageFile" };

    public static readonly Error UnsupportedType =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.UnsupportedType",
            "Tipe file tidak didukung",
            Domain);

    public static readonly Error ContentMismatch =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.ContentMismatch",
            "Isi file tidak sesuai dengan tipe file yang dikirim",
            Domain) with { Field = "ContentImageFile" };

    public static readonly Error InvalidImage =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.InvalidImage",
            "File gambar rusak atau tidak valid",
            Domain) with { Field = "ContentImageFile" };

    public static readonly Error ImageTooLarge =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.ImageTooLarge",
            "Resolusi gambar terlalu besar",
            Domain) with { Field = "ContentImageFile" };

    public static readonly Error InvalidPdf =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.InvalidPdf",
            "File PDF rusak atau terenkripsi",
            Domain);

    public static readonly Error PdfEmpty =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.PdfEmpty",
            "File PDF tidak memiliki halaman",
            Domain);

    public static readonly Error PdfTooManyPages =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.PdfTooManyPages",
            "Jumlah halaman PDF melebihi batas yang diizinkan",
            Domain);

    public static readonly Error ProcessingFailed =
        Error.Create(
            ErrorType.BadRequest,
            $"{Domain}.ProcessingFailed",
            "File tidak dapat diproses",
            Domain);
}