using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Enums;
using BeasiswaIn.Web.Services.Scholarships;

namespace BeasiswaIn.Web.Common.Errors;

internal static class ScholarshipServiceError
{
    private const string Domain = nameof(ScholarshipService);

    public static readonly Error NameDuplicated =
        Error.Create(
            ErrorType.NotFound,
            $"{Domain}.NameDuplicated",
            "Nama ini sudah digunakan",
            Domain) with { Field = "Name"};

    public static readonly Error NotFound =
        Error.Create(
            ErrorType.NotFound,
            $"{Domain}.NotFound",
            "Beasiswa ini tidak ditemukan",
            Domain);

    public static readonly Error CurrentlyInUse =
        Error.Create(
            ErrorType.NotFound,
            $"{Domain}.CurrentlyInUse",
            "Kategori tidak bisa dihapus karena sedang digunakan",
            Domain);
}